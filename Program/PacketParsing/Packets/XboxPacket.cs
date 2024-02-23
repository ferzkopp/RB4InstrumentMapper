using System;

namespace RB4InstrumentMapper.Parsing
{
    internal ref struct XboxPacket
    {
        public bool DirectionIn;
        public DateTime Time;
        public ReadOnlySpan<byte> Header;
        public ReadOnlySpan<byte> Data;

        public XboxPacket(ReadOnlySpan<byte> data, bool directionIn)
        {
            Data = data;
            DirectionIn = directionIn;

            Header = ReadOnlySpan<byte>.Empty;
            Time = DateTime.Now;
        }

        public static bool TryParse(ReadOnlySpan<char> input, out XboxPacket packet)
        {
            packet = default;
            if (input.IsEmpty)
                return false;

            // Skip time and packet length
            // For easier manual packet log construction, these are optional
            int lastBracket = input.LastIndexOf(']');
            if (lastBracket >= 0)
                input = input.Slice(lastBracket).TrimStart();

            // Parse direction
            // For easier manual packet log construction, this defaults to in
            bool directionIn = true;
            if (input.StartsWith(inStr.AsSpan()))
            {
                directionIn = true;
                input = input.Slice(inStr.Length).TrimStart();
            }
            else if (input.StartsWith(outStr.AsSpan()))
            {
                input = input.Slice(outStr.Length).TrimStart();
                directionIn = false;
            }

            // Skip header data if present
            int headerSeparator = input.LastIndexOf('|');
            if (headerSeparator >= 0)
                input = input.Slice(headerSeparator).TrimStart();

            // Parse data
            if (!ParsingUtils.TryParseBytesFromHexString(input, out byte[] bytes))
                return false;

            packet = new XboxPacket(bytes, directionIn);
            return true;
        }

        private const string inStr = "->";
        private const string outStr = "<-";

        public override string ToString()
        {
            return Header.IsEmpty
                ? $"[{Time:yyyy-MM-dd hh:mm:ss.fff}] [{Data.Length:D2}] {(DirectionIn ? inStr : outStr)} {ParsingUtils.ToHexString(Data)}"
                : $"[{Time:yyyy-MM-dd hh:mm:ss.fff}] [{Header.Length + Data.Length:D2}] {(DirectionIn ? inStr : outStr)} {ParsingUtils.ToHexString(Header)} | {ParsingUtils.ToHexString(Data)}";
        }
    }
}