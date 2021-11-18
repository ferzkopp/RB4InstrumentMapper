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
        static private vJoy.JoystickState iReport;

        [Flags]
        /// <summary>
        /// Button flags enum.
        /// </summary>
        private enum Buttons : uint
        {
            // Roughly follows the layout of an Xbox 360 guitar as viewed through Joy.cpl
            GreenFret = 0x0001, // Button 1
            RedFret = 0x0002, // Button 2
            BlueFret = 0x0004, // Button 3
            YellowFret = 0x0008, // Button 4
            OrangeFret = 0x0010, // Button 5

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
        */

        /*
        Continuous PoV hat reference:
            Ranges from 0 to 35999 (measured in 1/100 of a degree), goes clockwise

            None = 0xFFFFFFFF,
            Up = 0,
            Down = 18000,
            Left = 27000,
            Right = 9000
        */

        /// <summary>
        /// Maps a GuitarPacket to a vJoy device.
        /// </summary>
        /// <param name="packet">The pre-analyzed data packet to map.</param>
        /// <param name="vjoyClient">The vJoy client object reference to use.</param>
        /// <param name="joystickDeviceIndex">The vJoy device ID to map to.</param>
        /// <param name="instrumentId">The ID of the instrument being mapped.</param>
        /// <returns>True if packet was used and converted, false otherwise.</returns>
        public static bool MapPacket(in GuitarPacket packet, vJoy vjoyClient, uint joystickDeviceIndex, uint instrumentId)
        {
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

            // Reset report and assign device index
            iReport = new vJoy.JoystickState();
            iReport.bDevice = (byte)joystickDeviceIndex;

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

            // D-pad
            // Create an X-Y system for converting 4-direction d-pad to continuous PoV hat (using only 8 directions)
            int x = 0; // Left (-), Right (+)
            int y = 0; // Up (+), Down (-)
            // Left + Right will cancel each other out, same with Up/Down

            y += packet.DpadUp ? 1 : 0;
            y -= packet.DpadDown ? 1 : 0;
            x -= packet.DpadLeft ? 1 : 0;
            x += packet.DpadRight ? 1 : 0;

            // Ternaray operator mess:
            iReport.bHats = (
                // If d-pad is unpressed,
                (x + y == 0)
                // set the PoV hat to neutral.
                ? 0xFFFFFFFF
                // Else,
                : (
                    // If left/right are pressed,
                    (x != 0
                    // set the PoV hat to either left or right, plus or minus 45 degrees if up or down are pressed.
                    ? (uint)(18000 - (9000 * x) - (4500 * (y * x)))
                    // Else,
                    :
                        // If up/down are pressed,
                        (y != 0
                        // set the PoV hat to up or down.
                        ? (uint)(9000 - (9000 * y))
                        // Else, set the d-pad to neutral.
                        // (Should be impossible to get here, but the ternary operator requires an else. So be it.)
                        : 0xFFFFFFFF
                        )
                    )
                )
            );

            // Fret Green
            if (packet.UpperGreen || packet.LowerGreen)
            {
                iReport.Buttons |= (uint)Buttons.GreenFret;
            }
            // Fret Red
            if (packet.UpperRed || packet.LowerRed)
            {
                iReport.Buttons |= (uint)Buttons.RedFret;
            }
            // Fret Yellow
            if (packet.UpperYellow || packet.LowerYellow)
            {
                iReport.Buttons |= (uint)Buttons.YellowFret;
            }
            // Fret Blue
            if (packet.UpperBlue || packet.LowerBlue)
            {
                iReport.Buttons |= (uint)Buttons.BlueFret;
            }
            // Fret Orange
            if (packet.UpperOrange || packet.LowerOrange)
            {
                iReport.Buttons |= (uint)Buttons.OrangeFret;
            }

            // Map pickup switch to X-axis
            // Multiply the byte into a 32-bit uint, then subtract the max positive of an int + 1 to get a signed int
            iReport.AxisX = (int)((packet.PickupSwitch * 16843009) - 2147483648);

            // Map whammy to Y-axis
            iReport.AxisY = (int)((packet.WhammyBar * 16843009) - 2147483648);

            // Map tilt to Z-axis
            iReport.AxisZ = (int)((packet.Tilt * 16843009) - 2147483648);


            // Send data
            vjoyClient.UpdateVJD(joystickDeviceIndex, ref iReport);

            // Packet handled
            return true;
        }
    }
}
