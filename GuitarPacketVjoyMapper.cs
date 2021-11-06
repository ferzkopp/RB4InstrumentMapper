using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Maps RockBand 4 Guitar packets from WinPcap to vJoy virtual HID.
    /// </summary>
    public class GuitarPacketVjoyMapper
    {
        /// <summary>
        /// Packet definitions for Frets
        /// </summary>
        [Flags]
        public enum Frets : byte
        {
            Green = 0x01,
            Red = 0x02,
            Yellow = 0x04,
            Blue = 0x08,
            Orange = 0x10,
        }

        /// <summary>
        /// Packet definitions for Buttons
        /// </summary>
        [Flags]
        public enum Buttons : byte
        {
            Xbox = 0x01,
            Menu = 0x04,
            Options = 0x08,
        }

        /// <summary>
        /// Packet definitions for Dpad
        /// </summary>
        [Flags]
        public enum Dpad : byte
        {
            Down  = 0x01,
            Up    = 0x02,
            Left  = 0x04,
            Right = 0x08,
        }

        /// <summary>
        /// Size of guitar packets
        /// </summary>
        private const int GuitarPacketLength = 40;

        /// <summary>
        /// Size of packet header
        /// </summary>
        private const int XboxHeaderLength = 22;


        /// <summary>
        /// Position in the packet from header
        /// </summary>
        public enum PacketPosition : int 
        {
            Buttons   = 8,
            Dpad      = 9,
            Tilt      = 10,
            Whammy    = 11,
            Slider    = 12,
            UpperFret = 13,
            LowerFret = 14,
        }

        /// <summary>
        /// Joystick state
        /// </summary>
        static private vJoy.JoystickState iReport;

        /// <summary>
        /// Analyze packet and map it to vJoy buttons.
        /// </summary>
        /// <param name="packet">The data packet</param>
        /// <param name="joystickDevices">The vJoy object to map to</param>
        /// <param name="joystickDeviceIndex">The vJoy device id to map to</param>
        /// <param name="instrumentId">The instrument ID</param>
        /// <returns>True of packet was used and converted, false otherwise.</returns>
        public static bool AnalyzeAndMap(byte[] packet, vJoy joystickDevices, uint joystickDeviceIndex, byte[] instrumentId = null)
        {
            // Check packet
            if (packet != null && packet.Length == GuitarPacketLength)
            {
                // Need to match instrument ID?
                if (instrumentId != null && instrumentId.Length == 4)
                {
                    // Match ID ...
                    if (instrumentId[0] != packet[15] ||
                        instrumentId[1] != packet[14] ||
                        instrumentId[2] != packet[13] ||
                        instrumentId[3] != packet[12])
                    {
                        // ... no match
                        return false;
                    }
                }

                // Reset buttons
                iReport.Buttons = 0;
                iReport.bDevice = (byte)joystickDeviceIndex;

                // Map buttons
                byte buttons = packet[XboxHeaderLength + (int)PacketPosition.Buttons];

                // Menu
                bool menuButton = (buttons & (byte)Buttons.Menu) != 0;
                if (menuButton)
                {
                    iReport.Buttons |= (uint)1 << 14;
                }

                // Options
                bool optionsButton = (buttons & (byte)Buttons.Options) != 0;
                if (optionsButton)
                {
                    iReport.Buttons |= (uint)1 << 15;
                }

                // Xbox - not mapped

                // Map Dpad
                byte dpad = packet[XboxHeaderLength + (int)PacketPosition.Dpad];

                // Dpad Up
                bool dpadUpButton = (dpad & (byte)Dpad.Up) != 0;
                if (dpadUpButton)
                {
                    iReport.Buttons |= (uint)1 << 10;
                }

                // Dpad Down
                bool dpadDownButton = (dpad & (byte)Dpad.Down) != 0;
                if (dpadDownButton)
                {
                    iReport.Buttons |= (uint)1 << 11;
                }

                // Dpad Left
                bool dpadLeftButton = (dpad & (byte)Dpad.Left) != 0;
                if (dpadLeftButton)
                {
                    iReport.Buttons |= (uint)1 << 12;
                }

                // Dpad Right
                bool dpadRightButton = (dpad & (byte)Dpad.Right) != 0;
                if (dpadRightButton)
                {
                    iReport.Buttons |= (uint)1 << 13;
                }

                // Frets
                byte upperFret = packet[XboxHeaderLength + (int)PacketPosition.UpperFret];
                byte lowerFret = packet[XboxHeaderLength + (int)PacketPosition.LowerFret];

                // Fret Green
                bool fretGreen =
                    (upperFret & (byte)Frets.Green) != 0 ||
                    (lowerFret & (byte)Frets.Green) != 0;
                if (fretGreen)
                {
                    iReport.Buttons |= (uint)1 << 1;
                }

                // Fret Red
                bool fretRed =
                    (upperFret & (byte)Frets.Red) != 0 ||
                    (lowerFret & (byte)Frets.Red) != 0;
                if (fretRed)
                {
                    iReport.Buttons |= (uint)1 << 2;
                }

                // Fret Yellow
                bool fretYellow =
                    (upperFret & (byte)Frets.Yellow) != 0 ||
                    (lowerFret & (byte)Frets.Yellow) != 0;
                if (fretYellow)
                {
                    iReport.Buttons |= (uint)1 << 3;
                }

                // Fret Blue
                bool fretBlue =
                    (upperFret & (byte)Frets.Blue) != 0 ||
                    (lowerFret & (byte)Frets.Blue) != 0;
                if (fretBlue)
                {
                    iReport.Buttons |= (uint)1 << 4;
                }

                // Fret Orange
                bool fretOrange =
                    (upperFret & (byte)Frets.Orange) != 0 ||
                    (lowerFret & (byte)Frets.Orange) != 0;
                if (fretOrange)
                {
                    iReport.Buttons |= (uint)1 << 5;
                }

                // Map slider to X-axis
                byte slider = packet[XboxHeaderLength + (int)PacketPosition.Slider];
                int xAxis = slider;
                xAxis *= 128;
                iReport.AxisX = xAxis;

                // Map whammy to Y-axis
                byte whammy = packet[XboxHeaderLength + (int)PacketPosition.Whammy];
                int yAxis = whammy;
                yAxis *= 128;
                iReport.AxisY = yAxis;

                // Map tilt to Z-axis
                byte tilt = packet[XboxHeaderLength + (int)PacketPosition.Tilt];
                int zAxis = tilt;
                zAxis *= 128;
                iReport.AxisZ = zAxis;

                // Send data
                joystickDevices.UpdateVJD(joystickDeviceIndex, ref iReport);

                // Packet handled
                return true;
            }

            // Packet ignored
            return false;
        }
    }
}
