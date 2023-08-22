using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Re-definitions for button flags that have specific meanings.
    /// </summary>
    [Flags]
    internal enum DrumButton : ushort
    {
        // Not used as these are for menu navigation purposes
        // RedPad = GamepadButton.B,
        // GreenPad = GamepadButton.A,
        KickOne = GamepadButton.LeftBumper,
        KickTwo = GamepadButton.RightBumper
    }

    /// <summary>
    /// An input report from a drumkit.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DrumInput
    {
        public ushort Buttons;
        private readonly ushort pads;
        private readonly ushort cymbals;

        public byte RedPad => (byte)((pads & 0x00F0) >> 4);
        public byte YellowPad => (byte)(pads & 0x000F);
        public byte BluePad => (byte)((pads & 0xF000) >> 12);
        public byte GreenPad => (byte)((pads & 0x0F00) >> 8);

        public byte YellowCymbal => (byte)((cymbals & 0x00F0) >> 4);
        public byte BlueCymbal => (byte)(cymbals & 0x000F);
        public byte GreenCymbal => (byte)((cymbals & 0xF000) >> 12);
    }
}