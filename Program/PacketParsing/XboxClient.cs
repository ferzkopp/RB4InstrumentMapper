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
        /// The descriptor of the client.
        /// </summary>
        public XboxDescriptor Descriptor { get; private set; }

        /// <summary>
        /// The ID of the client.
        /// </summary>
        public byte ClientId { get; }

        private IDeviceMapper deviceMapper;

        private readonly Dictionary<CommandId, byte> previousSequenceIds = new Dictionary<CommandId, byte>();
        private readonly Dictionary<CommandId, ChunkBuffer> chunkBuffers = new Dictionary<CommandId, ChunkBuffer>()
        {
            { CommandId.Descriptor, new ChunkBuffer() },
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

                    Parent.SendMessage(sendHeader, ref acknowledge);
                    header.Flags &= ~CommandFlags.NeedsAcknowledgement;
                }
            }

            // Don't handle the same packet twice
            if (!previousSequenceIds.TryGetValue(header.CommandId, out byte previousSequence))
            {
                previousSequenceIds.Add(header.CommandId, header.SequenceCount);
            }
            else if (header.SequenceCount == previousSequence)
            {
                return XboxResult.Success;
            }

            switch (header.CommandId)
            {
                // Commands to ignore
                case CommandId.Acknowledgement:
                case CommandId.Authentication:
                case CommandId.SerialNumber:
                    break;

                case CommandId.Arrival:
                    return HandleArrival(commandData);

                case CommandId.Status:
                    return HandleStatus(commandData);

                case CommandId.Descriptor:
                    return HandleDescriptor(commandData);

                default:
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

                    // Hand off unrecognized commands to the mapper
                    return deviceMapper.HandlePacket(header.CommandId, commandData);
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
            return XboxResult.Success;
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
            return XboxResult.Success;
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