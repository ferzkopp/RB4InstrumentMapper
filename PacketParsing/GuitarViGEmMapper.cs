using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Functionality to map analyzed guitar packets to a ViGEmBus device.
    /// </summary>
    public class GuitarPacketViGEmMapper
    {
        /// <summary>
        /// Maps a GuitarPacket to a ViGEmBus Xbox 360 controller.
        /// </summary>
        /// <param name="packet">The pre-analyzed data packet.</param>
        /// <param name="vigemDevice">The ViGEmBus device to map to.</param>
        /// <param name="instrumentId">The instrument ID.</param>
        /// <returns>True if packet was mapped, false otherwise.</returns>
        public static bool AnalyzeAndMap(in GuitarPacket packet, IXbox360Controller vigemDevice, uint instrumentId)
        {
            // Don't auto-submit input reports for performance optimization
            if (vigemDevice.AutoSubmitReport == true)
            {
                vigemDevice.AutoSubmitReport = false;
            }

            // Ensure instrument ID is assigned ...
            if(instrumentId == 0)
            {
                // ... not assigned
                return false;
            }

            // Match instrument ID ...
            if (instrumentId != packet.InstrumentID)
            {
                // ... no match
                return false;
            }

            // Reset report
            vigemDevice.ResetReport();

            // Menu
            vigemDevice.SetButtonState(Xbox360Button.Start,
                                        packet.MenuButton);
            // Options
            vigemDevice.SetButtonState(Xbox360Button.Back,
                                        packet.OptionsButton);
            // Xbox
            vigemDevice.SetButtonState(Xbox360Button.Guide,
                                        packet.XboxButton);

            // Dpad Up
            vigemDevice.SetButtonState(Xbox360Button.Up,
                                        packet.DpadUp);
            // Dpad Down
            vigemDevice.SetButtonState(Xbox360Button.Down,
                                        packet.DpadDown);
            // Dpad Left
            vigemDevice.SetButtonState(Xbox360Button.Left,
                                        packet.DpadLeft);
            // Dpad Right
            vigemDevice.SetButtonState(Xbox360Button.Right,
                                        packet.DpadRight);

            // Fret Green
            vigemDevice.SetButtonState(Xbox360Button.A,
                                        packet.UpperGreen ||
                                        packet.LowerGreen);
            // Fret Red
            vigemDevice.SetButtonState(Xbox360Button.B,
                                        packet.UpperRed ||
                                        packet.LowerRed);
            // Fret Yellow
            vigemDevice.SetButtonState(Xbox360Button.Y,
                                        packet.UpperYellow ||
                                        packet.LowerYellow);
            // Fret Blue
            vigemDevice.SetButtonState(Xbox360Button.X,
                                        packet.UpperBlue ||
                                        packet.LowerBlue);
            // Fret Orange
            vigemDevice.SetButtonState(Xbox360Button.LeftShoulder,
                                        packet.UpperOrange ||
                                        packet.LowerOrange);
            // Solo fret flag
            vigemDevice.SetButtonState(Xbox360Button.LeftThumb,
                                        packet.LowerGreen ||
                                        packet.LowerRed ||
                                        packet.LowerYellow ||
                                        packet.LowerBlue ||
                                        packet.LowerOrange);

            // Whammy
            vigemDevice.SetAxisValue(Xbox360Axis.RightThumbX,
                                        (short)((packet.WhammyBar * 257) - 32768));
                                        // Multiply by 257 to scale into a ushort (255 * 257 - 65535),
                                        // then subtract by 32768 to shift into a signed short (65535 - 32768 = 32767, 0 - 32768 = -32768)

            // Tilt
            vigemDevice.SetAxisValue(Xbox360Axis.RightThumbY,
                                        (short)((packet.Tilt * 257) - 32768));
                                        // Multiply by 257 to scale into a ushort (255 * 257 - 65535),
                                        // then subtract by 32768 to shift into a signed short (65535 - 32768 = 32767, 0 - 32768 = -32768)

            // Pickup Switch
            vigemDevice.SetSliderValue(Xbox360Slider.LeftTrigger,
                                        packet.PickupSwitch);

            // Send data
            vigemDevice.SubmitReport();

            // Packet handled
            return true;
        }
    }
}
