using System;
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
    class XboxDevice
    {
        public static MappingMode MapperMode;

        /// <summary>
        /// Mapper interface to use.
        /// </summary>
        private IDeviceMapper deviceMapper;

        /// <summary>
        /// Buffer used to assemble chunked packets.
        /// </summary>
        private byte[] chunkBuffer;

        /// <summary>
        /// Creates a new XboxDevice with the given device ID and parsing mode.
        /// </summary>
        public XboxDevice()
        {
            switch (MapperMode)
            {
                case MappingMode.ViGEmBus:
                    deviceMapper = new VigemMapper();
                    break;

                case MappingMode.vJoy:
                    deviceMapper = new VjoyMapper();
                    break;
            }
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~XboxDevice()
        {
            Close();
        }

        /// <summary>
        /// Parses command data from a packet.
        /// </summary>
        public unsafe void ParseCommand(ReadOnlySpan<byte> commandData)
        {
            if (!CommandHeader.TryParse(commandData, out var header, out int bytesRead))
            {
                return;
            }
            commandData = commandData.Slice(bytesRead);

            // Chunked packets
            if ((header.Flags & CommandFlags.ChunkPacket) != 0)
            {
                // Get sequence length/index
                if (!ParsingUtils.DecodeLEB128(commandData, out int bufferIndex, out bytesRead))
                {
                    return;
                }
                commandData = commandData.Slice(bytesRead);

                // Do nothing with chunks of length 0
                if (bufferIndex > 0)
                {
                    // Buffer index equalling buffer length signals the end of the sequence
                    if (chunkBuffer != null && bufferIndex >= chunkBuffer.Length)
                    {
                        Debug.Assert(commandData.Length == 0);
                        commandData = chunkBuffer;
                    }
                    else
                    {
                        if ((header.Flags & CommandFlags.ChunkStart) != 0)
                        {
                            Debug.Assert(chunkBuffer == null);
                            // Buffer index is the total size of the buffer on the starting packet
                            chunkBuffer = new byte[bufferIndex];
                        }

                        Debug.Assert(chunkBuffer != null);
                        Debug.Assert((bufferIndex + commandData.Length) >= chunkBuffer.Length);
                        if (chunkBuffer == null || ((bufferIndex + commandData.Length) >= chunkBuffer.Length))
                        {
                            return;
                        }

                        commandData.CopyTo(chunkBuffer.AsSpan(bufferIndex, commandData.Length));
                        return;
                    }
                }
            }

            switch (header.CommandId)
            {
                case CommandId.Input:
                    deviceMapper.ParseInput(header, commandData.Slice(sizeof(CommandHeader)));
                    break;

                default:
                    // Don't do anything with unrecognized command IDs
                    break;
            }
        }

        /// <summary>
        /// Performs cleanup for the device.
        /// </summary>
        public void Close()
        {
            deviceMapper?.Close();
            deviceMapper = null;
        }
    }
}