using System;
using System.Runtime.InteropServices;
using RB4InstrumentMapper.Vjoy;
using vJoyInterfaceWrap;

#if DEBUG

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps gamepad inputs to a vJoy device.
    /// </summary>
    internal class GamepadVjoyMapper : VjoyMapper
    {
        public GamepadVjoyMapper(XboxClient client)
            : base(client)
        {
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxGamepadInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(XboxGamepadInput) || !MemoryMarshal.TryRead(data, out XboxGamepadInput gamepadReport))
                return XboxResult.InvalidMessage;

            HandleReport(ref state, gamepadReport);

            // Send data
            VjoyClient.UpdateDevice(deviceId, ref state);
            return XboxResult.Success;
        }

        /// <summary>
        /// Maps gamepad input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, XboxGamepadInput report)
        {
            // Buttons and axes are mapped the same way as they display in joy.cpl when used normally

            // Buttons
            state.SetButton(VjoyButton.One, report.A);
            state.SetButton(VjoyButton.Two, report.B);
            state.SetButton(VjoyButton.Three, report.X);
            state.SetButton(VjoyButton.Four, report.Y);

            state.SetButton(VjoyButton.Five, report.LeftBumper);
            state.SetButton(VjoyButton.Six, report.RightBumper);

            state.SetButton(VjoyButton.Seven, report.Options);
            state.SetButton(VjoyButton.Eight, report.Menu);

            state.SetButton(VjoyButton.Nine, report.LeftStickPress);
            state.SetButton(VjoyButton.Ten, report.RightStickPress);

            // D-pad
            ParseDpad(ref state, (XboxGamepadButton)report.Buttons);

            // Left stick
            SetAxis(ref state.AxisX, report.LeftStickX);
            SetAxisInverted(ref state.AxisY, report.LeftStickY);

            // Triggers
            // These are both combined into a single axis
            int triggerAxis = (report.LeftTrigger - report.RightTrigger) * 0x20;
            SetAxis(ref state.AxisZ, (short)triggerAxis);
        }
    }
}

#endif