using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Acknowledges a prior command and provides info about current buffer allocations.
    /// </summary>
    /// <remarks>
    /// Used for communication reliability and error detection.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Acknowledgement
    {
        private byte unk1;
        public CommandId InnerCommand;
        public byte InnerFlags_Client;
        public ushort BytesReceived;
        private ushort unk2;
        public ushort RemainingBuffer;

        public CommandFlags InnerFlags
        {
            get => (CommandFlags)(InnerFlags_Client & 0xF0);
            set => InnerFlags_Client = (byte)((byte)value & 0xF0 | InnerClient);
        }

        public byte InnerClient
        {
            get => (byte)(InnerFlags_Client & 0x0F);
            set => InnerFlags_Client = (byte)((byte)InnerFlags | value & 0x0F);
        }

        public static (CommandHeader header, Acknowledgement acknowledge) FromMessage(CommandHeader header,
            ReadOnlySpan<byte> messageBuffer)
        {
            // The Xbox One driver seems to always send this for the inner flag
            header.Flags = CommandFlags.SystemCommand;

            // Set acknowledgement data
            var acknowledge = new Acknowledgement()
            {
                unk1 = 0,
                InnerCommand = header.CommandId,
                InnerFlags_Client = header.Flags_Client,
                unk2 = 0,
                BytesReceived = (ushort)messageBuffer.Length,
            };

            // Set remaining header data (length is set when sending)
            header.CommandId = CommandId.Acknowledgement;

            return (header, acknowledge);
        }

        public static (CommandHeader header, Acknowledgement acknowledge) FromMessage(CommandHeader header,
            ReadOnlySpan<byte> messageBuffer, ChunkBuffer chunkBuffer)
        {
            var pair = FromMessage(header, messageBuffer);

            if (chunkBuffer.Buffer != null)
            {
                pair.acknowledge.BytesReceived = (ushort)chunkBuffer.BytesUsed;
                pair.acknowledge.RemainingBuffer = (ushort)chunkBuffer.BytesRemaining;
            }

            return pair;
        }
    }
}