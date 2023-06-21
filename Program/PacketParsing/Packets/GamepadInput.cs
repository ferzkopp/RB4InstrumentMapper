using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Flag definitions for the buttons bytes.
    /// </summary>
    [Flags]
    internal enum GamepadButton : ushort
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

#if DEBUG

    /// <summary>
    /// An input report from a drumkit.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct GamepadInput
    {
        public const ushort TriggerMax = 0x03FF;

        public bool A => (Buttons & (ushort)GamepadButton.A) != 0;
        public bool B => (Buttons & (ushort)GamepadButton.B) != 0;
        public bool X => (Buttons & (ushort)GamepadButton.X) != 0;
        public bool Y => (Buttons & (ushort)GamepadButton.Y) != 0;

        public bool DpadUp => (Buttons & (ushort)GamepadButton.DpadUp) != 0;
        public bool DpadDown => (Buttons & (ushort)GamepadButton.DpadDown) != 0;
        public bool DpadLeft => (Buttons & (ushort)GamepadButton.DpadLeft) != 0;
        public bool DpadRight => (Buttons & (ushort)GamepadButton.DpadRight) != 0;

        public bool LeftBumper => (Buttons & (ushort)GamepadButton.LeftBumper) != 0;
        public bool RightBumper => (Buttons & (ushort)GamepadButton.RightBumper) != 0;
        public bool LeftStickPress => (Buttons & (ushort)GamepadButton.LeftStickPress) != 0;
        public bool RightStickPress => (Buttons & (ushort)GamepadButton.RightStickPress) != 0;

        public bool Menu => (Buttons & (ushort)GamepadButton.Menu) != 0;
        public bool Options => (Buttons & (ushort)GamepadButton.Options) != 0;

        public ushort Buttons;
        public ushort LeftTrigger;
        public ushort RightTrigger;
        public short LeftStickX;
        public short LeftStickY;
        public short RightStickX;
        public short RightStickY;
    }

#endif
}