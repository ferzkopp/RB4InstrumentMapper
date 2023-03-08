using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    internal enum KeystrokeFlags
    {
        Pressed = 0x01,
    }

    public enum KeyCode : byte
    {
        LeftWindows = 0x5B, // Used for the guide button
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Keystroke
    {
        public byte Flags;
        public byte Keycode;

        public bool Pressed => (Flags & (byte)KeystrokeFlags.Pressed) != 0;
    }
}