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

        /// <summary>
        /// Maps a GuitarPacket to a vJoy device.
        /// </summary>
        /// <param name="packet">The pre-analyzed data packet to map.</param>
        /// <param name="vjoyClient">The vJoy client object reference to use.</param>
        /// <param name="joystickDeviceIndex">The vJoy device ID to map to.</param>
        /// <param name="instrumentId">The ID of the instrument being mapped.</param>
        /// <returns>True if packet was used and converted, false otherwise.</returns>
        public static bool MapPacket(in GuitarPacket packet, vJoy vjoyClient, uint joystickDeviceIndex, byte[] instrumentId = null)
        {
            byte[] packetId = ParsingHelpers.Int32HexStringToByteArray(packet.InstrumentIDString);

            // Need to match instrument ID?
            if (instrumentId != null && instrumentId.Length == 4)
            {
                // Match ID ...
                if (instrumentId[0] != packetId[0] ||
                    instrumentId[1] != packetId[1] ||
                    instrumentId[2] != packetId[2] ||
                    instrumentId[3] != packetId[3])
                {
                    // ... no match
                    return false;
                }
            }

            // Reset buttons
            iReport.Buttons = 0;
            iReport.bDevice = (byte)joystickDeviceIndex;

            // Menu
            if (packet.MenuButton)
            {
                iReport.Buttons |= 0x4000; // Button 15
            }
            // Options
            if (packet.OptionsButton)
            {
                iReport.Buttons |= 0x8000; // Button 16
            }
            // Xbox - not mapped

            // TODO: Look into using the PoV hat (iReport.bHats) instead of assigning d-pad to buttons
            // Dpad Up
            if (packet.DpadUp)
            {
                iReport.Buttons |= 0x0400; // Button 11
            }
            // Dpad Down
            if (packet.DpadDown)
            {
                iReport.Buttons |= 0x0800; // Button 12
            }
            // Dpad Left
            if (packet.DpadLeft)
            {
                iReport.Buttons |= 0x1000; // Button 13
            }
            // Dpad Right
            if (packet.DpadRight)
            {
                iReport.Buttons |= 0x2000; // Button 14
            }

            // Fret Green
            if (packet.UpperGreen || packet.LowerGreen)
            {
                iReport.Buttons |= 0x0001; // Button 1
            }
            // Fret Red
            if (packet.UpperRed || packet.LowerRed)
            {
                iReport.Buttons |= 0x0002; // Button 2
            }
            // Fret Yellow
            if (packet.UpperYellow || packet.LowerYellow)
            {
                iReport.Buttons |= 0x0004; // Button 3
            }
            // Fret Blue
            if (packet.UpperBlue || packet.LowerBlue)
            {
                iReport.Buttons |= 0x0008; // Button 4
            }
            // Fret Orange
            if (packet.UpperOrange || packet.LowerOrange)
            {
                iReport.Buttons |= 0x0010; // Button 5
            }

            // Map pickup switch to X-axis
            iReport.AxisX = packet.PickupSwitch * 128;

            // Map whammy to Y-axis
            iReport.AxisY = packet.WhammyBar * 128;

            // Map tilt to Z-axis
            iReport.AxisZ = packet.Tilt * 128;

            // Send data
            vjoyClient.UpdateVJD(joystickDeviceIndex, ref iReport);

            // Packet handled
            return true;
        }
    }
}
