using System;
using System.Runtime.InteropServices;
using RB4InstrumentMapper.Vjoy;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps guitar inputs to a vJoy device.
    /// </summary>
    internal class GuitarVjoyMapper : VjoyMapper
    {
        public GuitarVjoyMapper(XboxClient client)
            : base(client)
        {
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxGuitarInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!MemoryMarshal.TryRead(data, out XboxGuitarInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(ref state, guitarReport);
            return SubmitReport();
        }

        /// <summary>
        /// Maps guitar input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, XboxGuitarInput report)
        {
            // Menu and Options
            var buttons = (XboxGamepadButton)report.Buttons;
            state.SetButton(VjoyButton.Fifteen, (buttons & XboxGamepadButton.Menu) != 0);
            state.SetButton(VjoyButton.Sixteen, (buttons & XboxGamepadButton.Options) != 0);

            // D-pad
            ParseDpad(ref state, buttons);

            state.SetButton(VjoyButton.One, report.Green);
            state.SetButton(VjoyButton.Two, report.Red);
            state.SetButton(VjoyButton.Three, report.Yellow);
            state.SetButton(VjoyButton.Four, report.Blue);
            state.SetButton(VjoyButton.Five, report.Orange);

            // Whammy
            // Value ranges from 0 (not pressed) to 255 (fully pressed)
            SetAxis(ref state.AxisY, report.WhammyBar);

            // Tilt
            // Value ranges from 0 to 255
            // It seems to have a threshold of around 0x70 though,
            // after a certain point values will get floored to 0
            SetAxis(ref state.AxisZ, report.Tilt);

            // Pickup switch
            // Reported values are 0x00, 0x10, 0x20, 0x30, and 0x40 (ranges from 0 to 64)
            state.AxisX = report.PickupSwitch * 0x200;
        }
    }
}
