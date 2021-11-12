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

        /// <summary>
        /// Maps a DrumPacket to a vJoy device.
        /// </summary>
        /// <param name="packet">The pre-analyzed data packet to map.</param>
        /// <param name="vjoyClient">The vJoy client object reference to use.</param>
        /// <param name="joystickDeviceIndex">The vJoy device ID to map to.</param>
        /// <param name="instrumentId">The ID of the instrument being mapped.</param>
        /// <returns>True if packet was used and converted, false otherwise.</returns>
        public static bool MapPacket(in DrumPacket packet, vJoy vjoyClient, uint joystickDeviceIndex, uint instrumentId)
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

            // Red drum
            if (packet.RedDrum)
            {
                iReport.Buttons |= 0x0001; // Button 1
            }

            // Yellow drum
            if (packet.YellowDrum)
            {
                iReport.Buttons |= 0x0002; // Button 2
            }

            // Blue drum
            if (packet.BlueDrum)
            {
                iReport.Buttons |= 0x0004; // Button 3
            }

            // Green drum
            if (packet.GreenDrum)
            {
                iReport.Buttons |= 0x0008; // Button 4
            }

            // Bass drums
            if (packet.BassOne)
            {
                iReport.Buttons |= 0x0010; // Button 5
            }
            if (packet.BassTwo)
            {
                iReport.Buttons |= 0x0020; // Button 6
            }

            // Yellow cymbal
            if (packet.YellowCymbal)
            {
                iReport.Buttons |= 0x0040; // Button 7
            }

            // Blue cymbal
            if (packet.BlueCymbal)
            {
                iReport.Buttons |= 0x0080; // Button 8
            }

            // Green cymbal
            if (packet.GreenCymbal)
            {
                iReport.Buttons |= 0x0100; // Button 9
            }

            // Send data
            vjoyClient.UpdateVJD(joystickDeviceIndex, ref iReport);

            // Packet handled
            return true;
        }
    }
}
