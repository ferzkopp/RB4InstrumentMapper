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
        public DrumsVjoyMapper(XboxClient client, bool mapGuide)
            : base(client, mapGuide)
        {
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxDrumInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length != sizeof(XboxDrumInput) || !MemoryMarshal.TryRead(data, out XboxDrumInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(ref state, guitarReport);

            // Send data
            VjoyClient.UpdateDevice(deviceId, ref state);
            return XboxResult.Success;
        }

        /// <summary>
        /// Maps drumkit input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, XboxDrumInput report)
        {
            // Menu and Options
            var buttons = (XboxGamepadButton)report.Buttons;
            state.SetButton(VjoyButton.Fifteen, (buttons & XboxGamepadButton.Menu) != 0);
            state.SetButton(VjoyButton.Sixteen, (buttons & XboxGamepadButton.Options) != 0);

            // D-pad
            ParseDpad(ref state, buttons);

            // Face buttons
            state.SetButton(VjoyButton.Four, (buttons & XboxGamepadButton.A) != 0);
            state.SetButton(VjoyButton.One, (buttons & XboxGamepadButton.B) != 0);
            state.SetButton(VjoyButton.Three, (buttons & XboxGamepadButton.X) != 0);
            state.SetButton(VjoyButton.Two, (buttons & XboxGamepadButton.Y) != 0);

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
            state.SetButton(VjoyButton.Five, (report.Buttons & (ushort)XboxDrumButton.KickOne) != 0);
            state.SetButton(VjoyButton.Nine, (report.Buttons & (ushort)XboxDrumButton.KickTwo) != 0);
        }
    }
}
