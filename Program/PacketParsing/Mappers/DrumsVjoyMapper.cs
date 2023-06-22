using System;
using System.Runtime.InteropServices;
using RB4InstrumentMapper.Vjoy;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps drumkit inputs to a vJoy device.
    /// </summary>
    internal class DrumsVjoyMapper : VjoyMapper
    {
        public DrumsVjoyMapper() : base()
        {
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        protected override XboxResult OnPacketReceived(CommandId command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case CommandId.Input:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        /// <summary>
        /// Parses an input report.
        /// </summary>
        public unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length != sizeof(DrumInput) || !MemoryMarshal.TryRead(data, out DrumInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(ref state, guitarReport);

            // Send data
            VjoyClient.UpdateDevice(deviceId, ref state);
            return XboxResult.Success;
        }

        /// <summary>
        /// Maps drumkit input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, DrumInput report)
        {
            // Menu and Options
            var buttons = (GamepadButton)report.Buttons;
            state.SetButton(VjoyButton.Fifteen, (buttons & GamepadButton.Menu) != 0);
            state.SetButton(VjoyButton.Sixteen, (buttons & GamepadButton.Options) != 0);

            // D-pad
            ParseDpad(ref state, buttons);

            // Face buttons
            state.SetButton(VjoyButton.Four, (buttons & GamepadButton.A) != 0);
            state.SetButton(VjoyButton.One, (buttons & GamepadButton.B) != 0);
            state.SetButton(VjoyButton.Three, (buttons & GamepadButton.X) != 0);
            state.SetButton(VjoyButton.Two, (buttons & GamepadButton.Y) != 0);

            // Pads
            state.SetButton(VjoyButton.One, report.RedPad != 0);
            state.SetButton(VjoyButton.Two, report.YellowPad != 0);
            state.SetButton(VjoyButton.Three, report.BluePad != 0);
            state.SetButton(VjoyButton.Four, report.GreenPad != 0);

            // Cymbals
            state.SetButton(VjoyButton.Six, report.YellowCymbal != 0);
            state.SetButton(VjoyButton.Seven, report.BlueCymbal != 0);
            state.SetButton(VjoyButton.Eight, report.GreenCymbal != 0);

            // Kick pedals
            state.SetButton(VjoyButton.Five, (report.Buttons & (ushort)DrumButton.KickOne) != 0);
            state.SetButton(VjoyButton.Nine, (report.Buttons & (ushort)DrumButton.KickTwo) != 0);
        }
    }
}
