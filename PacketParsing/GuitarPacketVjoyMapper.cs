using System;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Functionality to map analyzed guitar packets to a vJoy device.
    /// </summary>
    public class GuitarPacketVjoyMapper
    {
        /// <summary>
        /// The vJoy device state.
        /// </summary>
        private static vJoy.JoystickState iReport;

        [Flags]
        /// <summary>
        /// Button flag definitions.
        /// </summary>
        private enum Buttons : uint
        {
            One = (uint)1 << 0,
            Two = (uint)1 << 1,
            Three = (uint)1 << 2,
            Four = (uint)1 << 3,
            Five = (uint)1 << 4,
            Six = (uint)1 << 5,
            Seven = (uint)1 << 6,
            Eight = (uint)1 << 7,
            Nine = (uint)1 << 8,
            Ten = (uint)1 << 9,
            Eleven = (uint)1 << 10,
            Twelve = (uint)1 << 11,
            Thirteen = (uint)1 << 12,
            Fourteen = (uint)1 << 13,
            Fifteen = (uint)1 << 14,
            Sixteen = (uint)1 << 15
        }

        /// <summary>
        /// Maps a GuitarPacket to a vJoy device.
        /// </summary>
        /// <param name="packet">The pre-analyzed data packet to map.</param>
        /// <param name="vjoyClient">The vJoy client to use.</param>
        /// <param name="joystickDeviceIndex">The vJoy device ID to map to.</param>
        /// <param name="instrumentId">The ID of the instrument being mapped.</param>
        /// <returns>True if packet was used and converted, false otherwise.</returns>
        public static bool MapPacket(GuitarPacket packet, vJoy vjoyClient, uint joystickDeviceIndex, uint instrumentId)
        {
            // Ensure instrument ID is assigned
            if (instrumentId == 0)
            {
                return false;
            }

            // Match instrument ID
            if (instrumentId != packet.InstrumentID)
            {
                return false;
            }

            // Reset report and assign device index
            iReport.Buttons = 0;
            iReport.bDevice = (byte)joystickDeviceIndex;

            // Face buttons
            // Menu
            if (packet.MenuButton)
            {
                iReport.Buttons |= (uint)Buttons.Fifteen;
            }

            // Options
            if (packet.OptionsButton)
            {
                iReport.Buttons |= (uint)Buttons.Sixteen;
            }

            // Xbox - not mapped
            // Ranges from 0 to 35999 (measured in 1/100 of a degree), clockwise, top 0

            // D-pad to POV
            if (packet.DpadUp)
            {
                if (packet.DpadLeft)
                {
                    iReport.bHats = 31500;
                }
                else if (packet.DpadRight)
                {
                    iReport.bHats = 4500;
                }
                else
                {
                    iReport.bHats = 0;
                }
            }
            else if (packet.DpadDown)
            {
                if (packet.DpadLeft)
                {
                    iReport.bHats = 22500;
                }
                else if (packet.DpadRight)
                {
                    iReport.bHats = 13500;
                }
                else
                {
                    iReport.bHats = 18000;
                }
            }
            else
            {
                if (packet.DpadLeft)
                {
                    iReport.bHats = 27000;
                }
                else if (packet.DpadRight)
                {
                    iReport.bHats = 9000;
                }
                else
                {
                    // Set the PoV hat to neutral
                    iReport.bHats = 0xFFFFFFFF;
                }
            }

            // Frets
            // Fret Green
            if (packet.UpperGreen || packet.LowerGreen)
            {
                iReport.Buttons |= (uint)Buttons.One;
            }
            // Fret Red
            if (packet.UpperRed || packet.LowerRed)
            {
                iReport.Buttons |= (uint)Buttons.Two;
            }
            // Fret Yellow
            if (packet.UpperYellow || packet.LowerYellow)
            {
                iReport.Buttons |= (uint)Buttons.Three;
            }
            // Fret Blue
            if (packet.UpperBlue || packet.LowerBlue)
            {
                iReport.Buttons |= (uint)Buttons.Four;
            }
            // Fret Orange
            if (packet.UpperOrange || packet.LowerOrange)
            {
                iReport.Buttons |= (uint)Buttons.Five;
            }

            // Axes

            // vJoy axis range is 0x0...0x7FFF(0...32767), 50 % = 0x4000(16384).

            // Map pickup switch to X-axis
            // input is 0, 16, 32, 48, and 64.
            int xAxis = packet.PickupSwitch;
            xAxis *= (32768 / 64);
            iReport.AxisX = xAxis;

            // Map whammy to Y-axis
            // input ranges from 0 (default) to 255 (depressed)
            int yAxis = packet.WhammyBar;
            yAxis *= (32768 / 256);
            iReport.AxisY = yAxis;

            // Map tilt to Z-axis
            // input ranges from 0 (horizontal) to 255 (vertical)
            int zAxis = packet.Tilt;
            zAxis *= (32768 / 256);
            iReport.AxisZ = zAxis;

            // Send data
            vjoyClient.UpdateVJD(joystickDeviceIndex, ref iReport);

            // Packet handled
            return true;
        }
    }
}
