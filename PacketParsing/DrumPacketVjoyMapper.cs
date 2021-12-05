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
        static private vJoy.JoystickState iReport;

        [Flags]
        /// <summary>
        /// Button flag definitions.
        /// </summary>
        private enum Buttons : uint
        {
            // For the pads, roughly follows the layout of an Xbox 360 drumkit as viewed through Joy.cpl
            GreenDrum = 0x0001, // Button 1
            RedDrum = 0x0002, // Button 2
            BlueDrum = 0x0004, // Button 3
            YellowDrum = 0x0008, // Button 4
            GreenCymbal = 0x0010, // Button 5
            BlueCymbal = 0x0020, // Button 6
            YellowCymbal = 0x0040, // Button 7
            BassOne = 0x0080, // Button 8
            BassTwo = 0x0100, // Button 9

            //Xbox = 0x2000, // Button 14
            Menu = 0x4000, // Button 15
            Options = 0x8000 // Button 16
        }

        /*
        Discrete PoV hat reference:
            None = 0xFFFFFFFF,
            Up = 0,
            Down = 2,
            Left = 3,
            Right = 1

        Continuous PoV hat reference:
            Ranges from 0 to 35999 (measured in 1/100 of a degree), goes clockwise

            None = 0xFFFFFFFF,
            Up = 0,
            Down = 18000,
            Left = 27000,
            Right = 9000
        */

        /// <summary>
        /// Maps a DrumPacket to a vJoy device.
        /// </summary>
        /// <param name="packet">The pre-analyzed data packet to map.</param>
        /// <param name="vjoyClient">The vJoy client to use.</param>
        /// <param name="joystickDeviceIndex">The vJoy device ID to use.</param>
        /// <param name="instrumentId">The ID of the instrument being mapped.</param>
        /// <returns>True if packet was used and converted, false otherwise.</returns>
        public static bool MapPacket(DrumPacket packet, vJoy vjoyClient, uint joystickDeviceIndex, uint instrumentId)
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
                iReport.Buttons |= (uint)Buttons.Menu;
            }

            // Options
            if (packet.OptionsButton)
            {
                iReport.Buttons |= (uint)Buttons.Options;
            }

            // Xbox - not mapped

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

            // Drums
            // Red drum
            if (packet.RedDrum)
            {
                iReport.Buttons |= (uint)Buttons.RedDrum;
            }

            // Yellow drum
            if (packet.YellowDrum)
            {
                iReport.Buttons |= (uint)Buttons.YellowDrum;
            }

            // Blue drum
            if (packet.BlueDrum)
            {
                iReport.Buttons |= (uint)Buttons.BlueDrum;
            }

            // Green drum
            if (packet.GreenDrum)
            {
                iReport.Buttons |= (uint)Buttons.GreenDrum;
            }

            // Bass 1
            if (packet.BassOne)
            {
                iReport.Buttons |= (uint)Buttons.BassOne;
            }

            // Bass 2
            if (packet.BassTwo)
            {
                iReport.Buttons |= (uint)Buttons.BassTwo;
            }


            // Cymbals
            // Yellow cymbal
            if (packet.YellowCymbal)
            {
                iReport.Buttons |= (uint)Buttons.YellowCymbal;
            }

            // Blue cymbal
            if (packet.BlueCymbal)
            {
                iReport.Buttons |= (uint)Buttons.BlueCymbal;
            }

            // Green cymbal
            if (packet.GreenCymbal)
            {
                iReport.Buttons |= (uint)Buttons.GreenCymbal;
            }


            // Send data
            vjoyClient.UpdateVJD(joystickDeviceIndex, ref iReport);

            // Packet handled
            return true;
        }
    }
}
