using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    public enum MappingMode
    {
        ViGEmBus = 1,
        vJoy = 2
    }

    /// <summary>
    /// Interface for Xbox devices.
    /// </summary>
    public class XboxDevice : IDisposable
    {
        public static MappingMode MapperMode;

        public ushort VendorId { get; private set; }
        public ushort ProductId { get; private set; }

        /// <summary>
        /// The descriptor of the device.
        /// </summary>
        public XboxDescriptor Descriptor { get; private set; }

        /// <summary>
        /// Mapper interface to use.
        /// </summary>
        private IDeviceMapper deviceMapper;

        /// <summary>
        /// Buffer used to assemble chunked packets.
        /// </summary>
        private byte[] chunkBuffer;

        /// <summary>
        /// The previous sequence ID for received command IDs.
        /// </summary>
        private readonly Dictionary<CommandId, byte> previousSequenceIds = new Dictionary<CommandId, byte>();

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~XboxDevice()
        {
            Dispose(false);
        }

        /// <summary>
        /// Handles an incoming packet for this device.
        /// </summary>
        public unsafe void HandlePacket(ReadOnlySpan<byte> data)
        {
            // Some devices may send multiple messages in a single packet, placing them back-to-back
            // The header length is very important in these scenarios, as it determines which bytes are part of the message
            // and where the next message's header begins.
            while (data.Length > 0)
            {
                // Command header
                if (!CommandHeader.TryParse(data, out var header, out int headerLength))
                {
                    return;
                }
                int messageLength = headerLength + header.DataLength;

                // Chunked messages
                if ((header.Flags & CommandFlags.ChunkPacket) != 0)
                {
                    if (!ParsingUtils.DecodeLEB128(data.Slice(headerLength), out int _, out int indexLength))
                    {
                        return;
                    }

                    messageLength += indexLength;
                }

                // Verify bounds
                if (data.Length < messageLength)
                {
                    return;
                }

                var messageData = data.Slice(0, messageLength);
                var commandData = messageData.Slice(headerLength); // Chunk index is not removed here, as message handling needs it
                HandleMessage(header, commandData);

                // Progress to next message
                data = data.Slice(messageData.Length);
            }
        }

        /// <summary>
        /// Parses command data from a packet.
        /// </summary>
        private unsafe void HandleMessage(CommandHeader header, ReadOnlySpan<byte> commandData)
        {
            // Chunked packets
            if ((header.Flags & CommandFlags.ChunkPacket) != 0)
            {
                if (!ProcessPacketChunk(header, ref commandData))
                {
                    // Chunk is ongoing or there was an error
                    return;
                }

                header.DataLength = commandData.Length;
                header.Flags &= ~(CommandFlags.ChunkPacket | CommandFlags.ChunkStart);
            }

            // Ensure lengths match
            if (header.DataLength != commandData.Length)
            {
                Debug.Fail($"Command header length does not match buffer length! Header: {header.DataLength}  Buffer: {commandData.Length}");
                return;
            }

            // Don't handle the same packet twice
            if (!previousSequenceIds.TryGetValue(header.CommandId, out byte previousSequence))
            {
                previousSequenceIds.Add(header.CommandId, header.SequenceCount);
            }
            else if (header.SequenceCount == previousSequence)
            {
                return;
            }

            switch (header.CommandId)
            {
                case CommandId.Arrival:
                    HandleArrival(commandData);
                    break;

                case CommandId.Descriptor:
                    HandleDescriptor(commandData);
                    break;

                default:
                    if (deviceMapper == null)
                    {
                        Console.WriteLine("Warning: This device was not encountered during its initial connection! It will use the fallback mapper instead of one specific to its device interface.");
                        Console.WriteLine("Consider hitting Start before connecting it to ensure correct behavior.");
                        deviceMapper = MapperFactory.GetFallbackMapper(MapperMode);
                    }

                    // Hand off unrecognized commands to the mapper
                    deviceMapper.HandlePacket(header.CommandId, commandData);
                    break;
            }
        }

        private unsafe bool ProcessPacketChunk(CommandHeader header, ref ReadOnlySpan<byte> chunkData)
        {
            // Get sequence length/index
            if (!ParsingUtils.DecodeLEB128(chunkData, out int bufferIndex, out int bytesRead))
            {
                return false;
            }
            chunkData = chunkData.Slice(bytesRead);

            // Verify packet length
            if (header.DataLength != chunkData.Length)
            {
                Debug.Fail($"Command header length does not match buffer length! Header: {header.DataLength}  Buffer: {chunkData.Length}");
                return false;
            }

            // Do nothing with chunks of length 0
            if (bufferIndex <= 0)
            {
                // Chunked packets with a length of 0 are valid and have been observed with Elite controllers
                bool emptySequence = bufferIndex == 0;
                Debug.Assert(emptySequence, $"Negative buffer index {bufferIndex}!");
                return emptySequence;
            }

            // Start of the chunk sequence
            if (chunkBuffer == null || (header.Flags & CommandFlags.ChunkStart) != 0)
            {
                // Safety check
                if ((header.Flags & CommandFlags.ChunkStart) == 0)
                {
                    Debug.Fail("Invalid chunk sequence start! No chunk buffer exists, expected a chunk start packet");
                    return false;
                }

                // Buffer index is the total size of the buffer on the starting packet
                chunkBuffer = new byte[bufferIndex];
                bufferIndex = 0;
            }

            // Buffer index equalling buffer length signals the end of the sequence
            if (bufferIndex >= chunkBuffer.Length)
            {
                // Safety checks
                if (bufferIndex > chunkBuffer.Length)
                {
                    Debug.Fail("Invalid chunk sequence end! Buffer index is beyond the end of the chunk buffer");
                    return false;
                }

                if (chunkData.Length != 0)
                {
                    Debug.Fail("Invalid chunk sequence end! Data was provided beyond the end of the buffer");
                    return false;
                }

                // Send off finished chunk buffer
                chunkData = chunkBuffer;
                chunkBuffer = null;
                return true;
            }

            // Verify chunk data bounds
            if ((bufferIndex + chunkData.Length) > chunkBuffer.Length)
            {
                Debug.Fail($"Invalid chunk sequence! Data was provided beyond the end of the buffer");
                return false;
            }

            // Copy data to buffer
            chunkData.CopyTo(chunkBuffer.AsSpan(bufferIndex, chunkData.Length));
            return false;
        }

        /// <summary>
        /// Handles the arrival message of the device.
        /// </summary>
        private unsafe void HandleArrival(ReadOnlySpan<byte> data)
        {
            if (VendorId != 0 || ProductId != 0)
                return;

            if (data.Length < sizeof(DeviceArrival) || MemoryMarshal.TryRead(data, out DeviceArrival arrival))
                return;

            VendorId = arrival.VendorId;
            ProductId = arrival.ProductId;
        }

        /// <summary>
        /// Handles the Xbox One descriptor of the device.
        /// </summary>
        private void HandleDescriptor(ReadOnlySpan<byte> data)
        {
            if (Descriptor != null)
                return;

            if (!XboxDescriptor.Parse(data, out var descriptor))
                return;

            Descriptor = descriptor;
            deviceMapper = MapperFactory.GetMapper(descriptor.InterfaceGuids, MapperMode);
        }

        /// <summary>
        /// Performs cleanup for the device.
        /// </summary>
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