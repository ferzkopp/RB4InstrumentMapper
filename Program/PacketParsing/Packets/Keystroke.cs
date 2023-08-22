using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    internal enum KeystrokeFlags : byte
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
        public KeystrokeFlags Flags;
        public KeyCode Keycode;

        public bool Pressed => (Flags & KeystrokeFlags.Pressed) != 0;
    }
}