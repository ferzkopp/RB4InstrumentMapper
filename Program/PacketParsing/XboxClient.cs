using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// A logical client on an Xbox device.
    /// </summary>
    internal class XboxClient : IDisposable
    {
        #region Message definitions
        private static readonly XboxMessage GetDescriptor = new XboxMessage()
        {
            Header = new CommandHeader()
            {
                CommandId = XboxDescriptor.CommandId,
                Flags = CommandFlags.SystemCommand,
            },
            // Header only, no data
        };

        private static readonly XboxMessage<DeviceConfiguration> PowerOnDevice = new XboxMessage<DeviceConfiguration>()
        {
            Header = new CommandHeader()
            {
                CommandId = DeviceConfiguration.CommandId,
                Flags = CommandFlags.SystemCommand,
            },
            Data = new DeviceConfiguration()
            {
                SubCommand = ConfigurationCommand.PowerOn,
            }
        };

        private static readonly XboxMessage<LedControl> EnableLed = new XboxMessage<LedControl>()
        {
            Header = new CommandHeader()
            {
                CommandId = LedControl.CommandId,
                Flags = CommandFlags.SystemCommand,
            },
            Data = new LedControl()
            {
                Mode = LedMode.On,
                Brightness = 0x14
            }
        };
        #endregion

        /// <summary>
        /// The parent device of the client.
        /// </summary>
        public XboxDevice Parent { get; }

        /// <summary>
        /// The descriptor of the client.
        /// </summary>
        public XboxDescriptor Descriptor { get; private set; }

        /// <summary>
        /// The ID of the client.
        /// </summary>
        public byte ClientId { get; }

        private IDeviceMapper deviceMapper;

        private readonly Dictionary<byte, byte> previousReceiveSequence = new Dictionary<byte, byte>();
        private readonly Dictionary<byte, byte> previousSendSequence = new Dictionary<byte, byte>();
        private readonly Dictionary<byte, ChunkBuffer> chunkBuffers = new Dictionary<byte, ChunkBuffer>()
        {
            { XboxDescriptor.CommandId, new ChunkBuffer() },
        };

        public XboxClient(XboxDevice parent, byte clientId)
        {
            Parent = parent;
            ClientId = clientId;
        }

        ~XboxClient()
        {
            Dispose(false);
        }

        /// <summary>
        /// Parses command data from a packet.
        /// </summary>
        internal unsafe XboxResult HandleMessage(CommandHeader header, ReadOnlySpan<byte> commandData)
        {
            // Verify packet length
            if (header.DataLength != commandData.Length)
            {
                Debug.Fail($"Command header length does not match buffer length! Header: {header.DataLength}  Buffer: {commandData.Length}");
                return XboxResult.InvalidMessage;
            }

            // Ensure acknowledgement happens regardless of pending/failure
            try
            {
                // Chunked packets
                if ((header.Flags & CommandFlags.ChunkPacket) != 0)
                {
                    if (!chunkBuffers.TryGetValue(header.CommandId, out var chunkBuffer))
                    {
                        chunkBuffer = new ChunkBuffer();
                        chunkBuffers.Add(header.CommandId, chunkBuffer);
                    }

                    var chunkResult = chunkBuffer.ProcessChunk(ref header, ref commandData);
                    switch (chunkResult)
                    {
                        case XboxResult.Success:
                            break;
                        case XboxResult.Pending: // Chunk is unfinished
                        default: // Error handling the chunk
                            return chunkResult;
                    }
                }
            }
            finally
            {
                // Acknowledgement
                if ((header.Flags & CommandFlags.NeedsAcknowledgement) != 0)
                {
                    var (sendHeader, acknowledge) = chunkBuffers.TryGetValue(header.CommandId, out var chunkBuffer)
                        ? Acknowledgement.FromMessage(header, commandData, chunkBuffer)
                        : Acknowledgement.FromMessage(header, commandData);

                    SendMessage(sendHeader, ref acknowledge);
                    header.Flags &= ~CommandFlags.NeedsAcknowledgement;
                }
            }

            // Don't handle the same packet twice
            if (!previousReceiveSequence.TryGetValue(header.CommandId, out byte previousSequence))
                previousSequence = 0;

            if (header.SequenceCount == previousSequence)
                return XboxResult.Success;
            previousReceiveSequence[header.CommandId] = header.SequenceCount;

            // System commands are handled directly
            if ((header.Flags & CommandFlags.SystemCommand) != 0)
                return HandleSystemCommand(header.CommandId, commandData);

            // Non-system commands are handled by the mapper
            if (deviceMapper == null)
            {
                deviceMapper = MapperFactory.GetFallbackMapper(XboxDevice.MapperMode);
                if (deviceMapper == null)
                {
                    // No more devices available, do nothing
                    return XboxResult.Success;
                }

                Console.WriteLine("Warning: This device was not encountered during its initial connection! It will use the fallback mapper instead of one specific to its device interface.");
                Console.WriteLine("Reconnect it (or hit Start before connecting it) to ensure correct behavior.");
            }

            return deviceMapper.HandlePacket(header.CommandId, commandData);
        }

        private XboxResult HandleSystemCommand(byte commandId, ReadOnlySpan<byte> commandData)
        {
            switch (commandId)
            {
                case DeviceArrival.CommandId:
                    return HandleArrival(commandData);

                case DeviceStatus.CommandId:
                    return HandleStatus(commandData);

                case XboxDescriptor.CommandId:
                    return HandleDescriptor(commandData);

                case Keystroke.CommandId:
                    return HandleKeystroke(commandData);
            }

            return XboxResult.Success;
        }

        /// <summary>
        /// Handles the arrival message of the device.
        /// </summary>
        private unsafe XboxResult HandleArrival(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(DeviceArrival) || MemoryMarshal.TryRead(data, out DeviceArrival arrival))
                return XboxResult.InvalidMessage;

            Console.WriteLine($"New client connected with ID {arrival.SerialNumber:X12}");

            // Kick off descriptor request
            return SendMessage(GetDescriptor);
        }

        /// <summary>
        /// Handles the arrival message of the device.
        /// </summary>
        private unsafe XboxResult HandleStatus(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(DeviceStatus) || !MemoryMarshal.TryRead(data, out DeviceStatus status))
                return XboxResult.InvalidMessage;

            if (!status.Connected)
                return XboxResult.Disconnected;

            return XboxResult.Success;
        }

        /// <summary>
        /// Handles the Xbox One descriptor of the device.
        /// </summary>
        private XboxResult HandleDescriptor(ReadOnlySpan<byte> data)
        {
            if (Descriptor != null)
                return XboxResult.Success;

            if (!XboxDescriptor.Parse(data, out var descriptor))
                return XboxResult.InvalidMessage;

            Descriptor = descriptor;
            deviceMapper = MapperFactory.GetMapper(descriptor.InterfaceGuids, XboxDevice.MapperMode);

            // Send final set of initialization messages
            var result = SendMessage(PowerOnDevice);
            if (result != XboxResult.Success)
                return result;

            result = SendMessage(EnableLed);
            if (result != XboxResult.Success)
                return result;

            // Authentication is not and will not be implemented, we just automatically pass all devices
            result = SendMessage(Authentication.SuccessMessage);
            if (result != XboxResult.Success)
                return result;

            return XboxResult.Success;
        }

        private unsafe XboxResult HandleKeystroke(ReadOnlySpan<byte> data)
        {
            if (data.Length % sizeof(Keystroke) != 0)
                return XboxResult.InvalidMessage;

            // Multiple keystrokes can be sent in a single message
            var keys = MemoryMarshal.Cast<byte, Keystroke>(data);
            foreach (var key in keys)
            {
                deviceMapper.HandleKeystroke(key);
            }

            return XboxResult.Success;
        }

        internal unsafe XboxResult SendMessage(XboxMessage message)
        {
            return SendMessage(message.Header, message.Data);
        }

        internal unsafe XboxResult SendMessage<T>(XboxMessage<T> message)
            where T : unmanaged
        {
            return SendMessage(message.Header, ref message.Data);
        }

        internal unsafe XboxResult SendMessage(CommandHeader header)
        {
            SetUpHeader(ref header);
            return Parent.SendMessage(header);
        }

        internal unsafe XboxResult SendMessage<T>(CommandHeader header, ref T data)
            where T : unmanaged
        {
            SetUpHeader(ref header);
            return Parent.SendMessage(header, ref data);
        }

        internal XboxResult SendMessage(CommandHeader header, Span<byte> data)
        {
            SetUpHeader(ref header);
            return Parent.SendMessage(header, data);
        }

        private void SetUpHeader(ref CommandHeader header)
        {
            header.Client = ClientId;

            if (!previousSendSequence.TryGetValue(header.CommandId, out byte sequence) ||
                sequence == 0xFF) // Sequence IDs of 0 are not valid
                sequence = 0;

            header.SequenceCount = ++sequence;
            previousSendSequence[header.CommandId] = sequence;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                deviceMapper?.Dispose();
                deviceMapper = null;
            }
        }
    }
}