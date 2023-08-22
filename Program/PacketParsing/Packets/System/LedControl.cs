using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    internal enum LedMode : byte
    {
        Off = 0x00,
        On = 0x01,
        BlinkFast = 0x02,
        BlinkNormal = 0x03,
        BlinkSlow = 0x04,
        FadeSlow = 0x08,
        FadeFast = 0x09,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LedControl
    {
        public const byte CommandId = 0x0a;

        private byte unknown;

        public LedMode Mode;
        public byte Brightness; 
    }
}
