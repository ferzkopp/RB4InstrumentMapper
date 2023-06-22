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
        public GuitarVjoyMapper() : base()
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
            if (data.Length != sizeof(GuitarInput) || !MemoryMarshal.TryRead(data, out GuitarInput guitarReport))
                return;

            HandleReport(ref state, guitarReport);

            // Send data
            VjoyClient.UpdateDevice(deviceId, ref state);
        }

        /// <summary>
        /// Maps guitar input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, GuitarInput report)
        {
            // Menu and Options
            var buttons = (GamepadButton)report.Buttons;
            state.SetButton(VjoyButton.Fifteen, (buttons & GamepadButton.Menu) != 0);
            state.SetButton(VjoyButton.Sixteen, (buttons & GamepadButton.Options) != 0);

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
