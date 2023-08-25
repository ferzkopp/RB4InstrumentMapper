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

        public override string ToString()
        {
            return Header.IsEmpty
                ? $"[{Time:yyyy-MM-dd hh:mm:ss.fff}] [{Data.Length:D2}] {(DirectionIn ? "->" : "<-")} {ParsingUtils.ToString(Data)}"
                : $"[{Time:yyyy-MM-dd hh:mm:ss.fff}] [{Header.Length + Data.Length:D2}] {(DirectionIn ? "->" : "<-")} {ParsingUtils.ToString(Header)} | {ParsingUtils.ToString(Data)}";
        }
    }
}