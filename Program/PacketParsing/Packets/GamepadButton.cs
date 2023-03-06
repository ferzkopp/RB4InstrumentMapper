using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Flag definitions for the buttons bytes.
    /// </summary>
    [Flags]
    public enum GamepadButton : ushort
    {
        Sync = 0x0001,
        Unused = 0x0002,
        Menu = 0x0004,
        Options = 0x0008,
        A = 0x0010,
        B = 0x0020,
        X = 0x0040,
        Y = 0x0080,
        DpadUp = 0x0100,
        DpadDown = 0x0200,
        DpadLeft = 0x0400,
        DpadRight = 0x0800,
        LeftBumper = 0x1000,
        RightBumper = 0x2000,
        LeftStickPress = 0x4000,
        RightStickPress = 0x8000
    }
}