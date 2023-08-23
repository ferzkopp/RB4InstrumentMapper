using System;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps guitar inputs to a ViGEmBus device.
    /// </summary>
    internal class GuitarVigemMapper : VigemMapper
    {
        public GuitarVigemMapper(XboxClient client, bool mapGuide)
            : base(client, mapGuide)
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
            if (data.Length != sizeof(XboxGuitarInput) || !MemoryMarshal.TryRead(data, out XboxGuitarInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, guitarReport);

            // Send data
            device.SubmitReport();
            return XboxResult.Success;
        }

        /// <summary>
        /// Maps guitar input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in XboxGuitarInput report)
        {
            // Menu and Options
            var buttons = (XboxGamepadButton)report.Buttons;
            device.SetButtonState(Xbox360Button.Start, (buttons & XboxGamepadButton.Menu) != 0);
            device.SetButtonState(Xbox360Button.Back, (buttons & XboxGamepadButton.Options) != 0);

            // Dpad
            device.SetButtonState(Xbox360Button.Up, (buttons & XboxGamepadButton.DpadUp) != 0);
            device.SetButtonState(Xbox360Button.Down, (buttons & XboxGamepadButton.DpadDown) != 0);
            device.SetButtonState(Xbox360Button.Left, (buttons & XboxGamepadButton.DpadLeft) != 0);
            device.SetButtonState(Xbox360Button.Right, (buttons & XboxGamepadButton.DpadRight) != 0);

            // Frets
            device.SetButtonState(Xbox360Button.A, report.Green);
            device.SetButtonState(Xbox360Button.B, report.Red);
            device.SetButtonState(Xbox360Button.Y, report.Yellow);
            device.SetButtonState(Xbox360Button.X, report.Blue);
            device.SetButtonState(Xbox360Button.LeftShoulder, report.Orange);

            // Lower fret flag
            device.SetButtonState(Xbox360Button.LeftThumb, report.LowerFretFlag);

            // Whammy
            device.SetAxisValue(Xbox360Axis.RightThumbX, report.WhammyBar.ScaleToInt16());
            // Tilt
            device.SetAxisValue(Xbox360Axis.RightThumbY, report.Tilt.ScaleToInt16());
            // Pickup Switch
            device.SetSliderValue(Xbox360Slider.LeftTrigger, report.PickupSwitch);
        }
    }
}
