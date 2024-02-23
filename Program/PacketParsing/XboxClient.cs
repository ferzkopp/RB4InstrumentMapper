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
        /// <summary>
        /// The parent device of the client.
        /// </summary>
        public XboxDevice Parent { get; }

        /// <summary>
        /// The arrival info of the client.
        /// </summary>
        public XboxArrival Arrival { get; private set; }

        /// <summary>
        /// The descriptor of the client.
        /// </summary>
        public XboxDescriptor Descriptor { get; private set; }

        /// <summary>
        /// The ID of the client.
        /// </summary>
        public byte ClientId { get; }

        private DeviceMapper deviceMapper;

        private bool receivedFirstMessage;

        private readonly Dictionary<byte, byte> previousReceiveSequence = new Dictionary<byte, byte>();
        private readonly Dictionary<byte, byte> previousSendSequence = new Dictionary<byte, byte>();
        private readonly Dictionary<byte, XboxChunkBuffer> chunkBuffers = new Dictionary<byte, XboxChunkBuffer>()
        {
            { XboxDescriptor.CommandId, new XboxChunkBuffer() },
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
        internal unsafe XboxResult HandleMessage(XboxCommandHeader header, ReadOnlySpan<byte> commandData)
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
                if ((header.Flags & XboxCommandFlags.ChunkPacket) != 0)
                {
                    if (!chunkBuffers.TryGetValue(header.CommandId, out var chunkBuffer))
                    {
                        chunkBuffer = new XboxChunkBuffer();
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
                if ((header.Flags & XboxCommandFlags.NeedsAcknowledgement) != 0)
                {
                    var (sendHeader, acknowledge) = chunkBuffers.TryGetValue(header.CommandId, out var chunkBuffer)
                        ? XboxAcknowledgement.FromMessage(header, commandData, chunkBuffer)
                        : XboxAcknowledgement.FromMessage(header, commandData);

                    SendMessage(sendHeader, ref acknowledge);
                    header.Flags &= ~XboxCommandFlags.NeedsAcknowledgement;
                }
            }

            // Don't handle the same packet twice
            if (!previousReceiveSequence.TryGetValue(header.CommandId, out byte previousSequence))
                previousSequence = 0;

            if (header.SequenceCount == previousSequence)
                return XboxResult.Success;
            previousReceiveSequence[header.CommandId] = header.SequenceCount;

            // System commands are handled directly
            XboxResult result;
            if ((header.Flags & XboxCommandFlags.SystemCommand) != 0)
            {
                result = HandleSystemCommand(header.CommandId, commandData);
            }
            else
            {
                // Non-system commands are handled by the mapper
                if (deviceMapper == null)
                {
                    deviceMapper = MapperFactory.GetFallbackMapper(this);
                    if (deviceMapper == null)
                    {
                        // No more devices available, do nothing
                        return XboxResult.Success;
                    }

                    PacketLogging.PrintMessage("Warning: This device was not encountered during its initial connection! It will use the fallback mapper instead of one specific to its device interface.");
                    PacketLogging.PrintMessage("Reconnect it (or hit Start before connecting it) to ensure correct behavior.");
                }

                result = deviceMapper.HandleMessage(header.CommandId, commandData);
            }

            receivedFirstMessage = true;
            return result;
        }

        private XboxResult HandleSystemCommand(byte commandId, ReadOnlySpan<byte> commandData)
        {
            switch (commandId)
            {
                case XboxArrival.CommandId:
                    return HandleArrival(commandData);

                case XboxStatus.CommandId:
                    return HandleStatus(commandData);

                case XboxDescriptor.CommandId:
                    return HandleDescriptor(commandData);

                case XboxKeystroke.CommandId:
                    return HandleKeystroke(commandData);
            }

            return XboxResult.Success;
        }

        /// <summary>
        /// Handles the arrival message of the device.
        /// </summary>
        private unsafe XboxResult HandleArrival(ReadOnlySpan<byte> data)
        {
            if (Arrival.SerialNumber != 0)
                return XboxResult.Success;

            if (!ParsingUtils.TryRead(data, out XboxArrival arrival))
                return XboxResult.InvalidMessage;

            // If we didn't receive the arrival as the first message, the device was likely reconnected
            // This happens when removing the batteries to turn off wireless controllers
            if (receivedFirstMessage)
                return XboxResult.Reconnected;

            PacketLogging.PrintMessage($"New client connected with ID {arrival.SerialNumber:X12}");
            Arrival = arrival;

            // Kick off descriptor request
            return SendMessage(XboxDescriptor.GetDescriptor);
        }

        /// <summary>
        /// Handles the arrival message of the device.
        /// </summary>
        private unsafe XboxResult HandleStatus(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxStatus status))
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
            deviceMapper = MapperFactory.GetMapper(this);
            if (deviceMapper == null)
            {
                // Device is unsupported
                SendMessage(XboxConfiguration.PowerOffDevice);
                return XboxResult.UnsupportedDevice;
            }

            // Send final set of initialization messages
            Debug.Assert(Descriptor.OutputCommands.Contains(XboxConfiguration.CommandId));
            var result = SendMessage(XboxConfiguration.PowerOnDevice);
            if (result != XboxResult.Success)
                return result;

            if (Descriptor.OutputCommands.Contains(XboxLedControl.CommandId))
            {
                result = SendMessage(XboxLedControl.EnableLed);
                if (result != XboxResult.Success)
                    return result;
            }

            if (Descriptor.OutputCommands.Contains(XboxAuthentication.CommandId))
            {
                // Authentication is not and will not be implemented, we just automatically pass all devices
                result = SendMessage(XboxAuthentication.SuccessMessage);
                if (result != XboxResult.Success)
                    return result;
            }

            return XboxResult.Success;
        }

        private unsafe XboxResult HandleKeystroke(ReadOnlySpan<byte> data)
        {
            if (data.Length % sizeof(XboxKeystroke) != 0)
                return XboxResult.InvalidMessage;

            // Multiple keystrokes can be sent in a single message
            var keys = MemoryMarshal.Cast<byte, XboxKeystroke>(data);
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

        internal unsafe XboxResult SendMessage(XboxCommandHeader header)
        {
            SetUpHeader(ref header);
            return Parent.SendMessage(header);
        }

        internal unsafe XboxResult SendMessage<T>(XboxCommandHeader header, ref T data)
            where T : unmanaged
        {
            SetUpHeader(ref header);
            return Parent.SendMessage(header, ref data);
        }

        internal XboxResult SendMessage(XboxCommandHeader header, Span<byte> data)
        {
            SetUpHeader(ref header);
            return Parent.SendMessage(header, data);
        }

        private void SetUpHeader(ref XboxCommandHeader header)
        {
            header.Client = ClientId;

            if (!previousSendSequence.TryGetValue(header.CommandId, out byte sequence) ||
                sequence == 0xFF) // Sequence IDs of 0 are not valid
                sequence = 0;

            header.SequenceCount = ++sequence;
            previousSendSequence[header.CommandId] = sequence;
        }

        public void EnableInputs(bool enabled)
        {
            deviceMapper?.EnableInputs(enabled);
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