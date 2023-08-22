using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Flags for keystroke events.
    /// </summary>
    internal enum KeystrokeFlags : byte
    {
        Pressed = 0x01,
    }

    /// <summary>
    /// Possible key codes.
    /// </summary>
    /// <remarks>
    /// These mirror those in the Win32 API; for brevity, only the ones used are defined here.
    /// </remarks>
    public enum KeyCode : byte
    {
        LeftWindows = 0x5B, // Used for the guide button
    }

    /// <summary>
    /// A keystroke event from a controller.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Keystroke
    {
        public KeystrokeFlags Flags;
        public KeyCode Keycode;

        public bool Pressed => (Flags & KeystrokeFlags.Pressed) != 0;
    }
}