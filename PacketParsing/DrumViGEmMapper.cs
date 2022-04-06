using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Functionality to map analyzed drumkit packets to a ViGEmBus device.
    /// </summary>
    public class DrumPacketViGEmMapper
    {
        /// <summary>
        /// Maps a DrumPacket to a ViGEmBus Xbox 360 controller.
        /// </summary>
        /// <param name="packet">The pre-analyzed data packet.</param>
        /// <param name="vigemDevice">The ViGEmBus device to map to.</param>
        /// <param name="instrumentId">The instrument ID.</param>
        /// <returns>True if packet was mapped, false otherwise.</returns>
        public static bool MapPacket(DrumPacket packet, IXbox360Controller vigemDevice, ulong instrumentId)
        {
            // Ensure instrument ID is assigned
            if(instrumentId == 0)
            {
                return false;
            }

            // Match instrument ID
            if (instrumentId != packet.InstrumentID)
            {
                return false;
            }

            // Don't auto-submit input reports for performance optimization
            if (vigemDevice.AutoSubmitReport)
            {
                vigemDevice.AutoSubmitReport = false;
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

            // Red
            vigemDevice.SetButtonState(Xbox360Button.B,
                                        packet.RedDrum);
            // Yellow
            vigemDevice.SetButtonState(Xbox360Button.Y,
                                        packet.YellowDrum ||
                                        packet.YellowCymbal);
            // Blue
            vigemDevice.SetButtonState(Xbox360Button.X,
                                        packet.BlueDrum ||
                                        packet.BlueCymbal);
            // Green
            vigemDevice.SetButtonState(Xbox360Button.A,
                                        packet.GreenDrum ||
                                        packet.GreenCymbal);

            // Pad Flag
            vigemDevice.SetButtonState(Xbox360Button.RightThumb,
                                        packet.RedDrum ||
                                        packet.YellowDrum ||
                                        packet.BlueDrum ||
                                        packet.GreenDrum);
            // Cymbal Flag
            vigemDevice.SetButtonState(Xbox360Button.RightShoulder,
                                        packet.YellowCymbal ||
                                        packet.BlueCymbal ||
                                        packet.GreenCymbal);

            // Bass One
            vigemDevice.SetButtonState(Xbox360Button.LeftShoulder,
                                        packet.BassOne);
            // Bass Two
            vigemDevice.SetButtonState(Xbox360Button.LeftThumb,
                                        packet.BassTwo);

            // Pad/cymbal velocities, for when those get researched
            /*
            // Red velocity
            vigemDevice.SetAxisValue(Xbox360Axis.LeftThumbX,
                                        packet.RedVelocity != 0 ? (short)((256 - packet.RedVelocity) * 128) : 0);
                                        // if packet velocity is not 0,
                                        // return the velocity inverted (i.e. 0 = hardest hit, 255 = softest)
                                        // and scaled to the positive half of a short
            // Yellow velocity
            vigemDevice.SetAxisValue(Xbox360Axis.LeftThumbY,
                                        packet.YellowVelocity != 0 ? (short)((256 - packet.YellowVelocity) * -128) : 0);
                                        // if packet velocity is not 0,
                                        // return the velocity inverted (i.e. 0 = hardest hit, 255 = softest)
                                        // and scaled to the negative half of a short
            // Blue velocity
            vigemDevice.SetAxisValue(Xbox360Axis.LeftThumbX,
                                        packet.BlueVelocity != 0 ? (short)((256 - packet.BlueVelocity) * 128) : 0);
                                        // same as Red
            // Green velocity
            vigemDevice.SetAxisValue(Xbox360Axis.LeftThumbX,
                                        packet.GreenVelocity != 0 ? (short)((256 - packet.GreenVelocity) * -128) : 0);
                                        // same as Yellow
            */

            // Send data
            vigemDevice.SubmitReport();

            // Packet handled
            return true;
        }
    }
}
