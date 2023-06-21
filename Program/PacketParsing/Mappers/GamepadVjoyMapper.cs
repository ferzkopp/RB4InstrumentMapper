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
        public GamepadVjoyMapper() : base()
        {
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        protected override void OnPacketReceived(CommandId command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case CommandId.Input:
                    ParseInput(data);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Parses an input report.
        /// </summary>
        public unsafe void ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(GamepadInput) || !MemoryMarshal.TryRead(data, out GamepadInput gamepadReport))
                return;

            HandleReport(ref state, gamepadReport);

            // Send data
            VjoyClient.UpdateDevice(deviceId, ref state);
        }

        /// <summary>
        /// Maps gamepad input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, GamepadInput report)
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
            ParseDpad(ref state, (GamepadButton)report.Buttons);

            // Sticks
            state.AxisX = report.LeftStickX.ScaleToInt32();
            state.AxisY = report.LeftStickY.ScaleToInt32();
            state.AxisXRot = report.RightStickX.ScaleToInt32();
            state.AxisYRot = report.RightStickY.ScaleToInt32();

            // Triggers
            // These are both combined into a single axis
            state.AxisZ = (report.RightTrigger - report.LeftTrigger) * 0x40100401; // Special scaling, since the triggers are 10-bit values
        }
    }
}

#endif