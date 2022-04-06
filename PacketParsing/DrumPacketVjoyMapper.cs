using System;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Functionality to map analyzed drumkit packets to a vJoy device.
    /// </summary>
    public class DrumPacketVjoyMapper
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
        /// Maps a DrumPacket to a vJoy device.
        /// </summary>
        /// <param name="packet">The pre-analyzed data packet to map.</param>
        /// <param name="vjoyClient">The vJoy client to use.</param>
        /// <param name="joystickDeviceIndex">The vJoy device ID to use.</param>
        /// <param name="instrumentId">The ID of the instrument being mapped.</param>
        /// <returns>True if packet was used and converted, false otherwise.</returns>
        public static bool MapPacket(DrumPacket packet, vJoy vjoyClient, uint joystickDeviceIndex, ulong instrumentId)
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

            // D-pad to POV
            // Ranges from 0 to 35999 (measured in 1/100 of a degree), clockwise, top 0
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

            // Drums
            // Red drum
            if (packet.RedDrum)
            {
                iReport.Buttons |= (uint)Buttons.One;
            }

            // Yellow drum
            if (packet.YellowDrum)
            {
                iReport.Buttons |= (uint)Buttons.Two;
            }

            // Blue drum
            if (packet.BlueDrum)
            {
                iReport.Buttons |= (uint)Buttons.Three;
            }

            // Green drum
            if (packet.GreenDrum)
            {
                iReport.Buttons |= (uint)Buttons.Four;
            }

            // Bass 1
            if (packet.BassOne)
            {
                iReport.Buttons |= (uint)Buttons.Five;
            }

            // Bass 2
            if (packet.BassTwo)
            {
                iReport.Buttons |= (uint)Buttons.Nine;
            }


            // Cymbals
            // Yellow cymbal
            if (packet.YellowCymbal)
            {
                iReport.Buttons |= (uint)Buttons.Six;
            }

            // Blue cymbal
            if (packet.BlueCymbal)
            {
                iReport.Buttons |= (uint)Buttons.Seven;
            }

            // Green cymbal
            if (packet.GreenCymbal)
            {
                iReport.Buttons |= (uint)Buttons.Eight;
            }

            // Send data
            vjoyClient.UpdateVJD(joystickDeviceIndex, ref iReport);

            // Packet handled
            return true;
        }
    }
}
