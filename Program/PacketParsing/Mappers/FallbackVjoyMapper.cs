using System;
using System.Runtime.InteropServices;
using RB4InstrumentMapper.Vjoy;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// The vJoy mapper used when device type could not be determined. Maps based on report length.
    /// </summary>
    class FallbackVjoyMapper : VjoyMapper
    {
        public FallbackVjoyMapper() : base()
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
            if (data.Length == sizeof(GuitarInput) && MemoryMarshal.TryRead(data, out GuitarInput guitarReport))
            {
                ParseGuitar(guitarReport);
            }
            else if (data.Length == sizeof(DrumInput) && MemoryMarshal.TryRead(data, out DrumInput drumReport))
            {
                ParseDrums(drumReport);
            }
            else
            {
                // Not handled
                return;
            }

            // Send data
            VjoyClient.UpdateDevice(deviceId, ref state);
        }

        /// <summary>
        /// Parses guitar input data from an input report.
        /// </summary>
        private void ParseGuitar(GuitarInput report)
        {
            // Menu and Options
            var buttons = (GamepadButton)report.Buttons;
            state.SetButton(VjoyButton.Fifteen, (buttons & GamepadButton.Menu) != 0);
            state.SetButton(VjoyButton.Sixteen, (buttons & GamepadButton.Options) != 0);

            // D-pad
            ParseDpad(buttons);

            state.SetButton(VjoyButton.One, report.Green);
            state.SetButton(VjoyButton.Two, report.Red);
            state.SetButton(VjoyButton.Three, report.Yellow);
            state.SetButton(VjoyButton.Four, report.Blue);
            state.SetButton(VjoyButton.Five, report.Orange);

            // Whammy
            // Value ranges from 0 (not pressed) to 255 (fully pressed)
            state.AxisY = report.WhammyBar.ScaleToInt32();

            // Tilt
            // Value ranges from 0 to 255
            // It seems to have a threshold of around 0x70 though,
            // after a certain point values will get floored to 0
            state.AxisZ = report.Tilt.ScaleToInt32();

            // Pickup switch
            // Reported values are 0x00, 0x10, 0x20, 0x30, and 0x40 (ranges from 0 to 64)
            state.AxisX = report.PickupSwitch.ScaleToInt32();
        }

        /// <summary>
        /// Parses drums input data from an input report.
        /// </summary>
        private void ParseDrums(DrumInput report)
        {
            // Menu and Options
            var buttons = (GamepadButton)report.Buttons;
            state.SetButton(VjoyButton.Fifteen, (buttons & GamepadButton.Menu) != 0);
            state.SetButton(VjoyButton.Sixteen, (buttons & GamepadButton.Options) != 0);

            // D-pad
            ParseDpad(buttons);

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
