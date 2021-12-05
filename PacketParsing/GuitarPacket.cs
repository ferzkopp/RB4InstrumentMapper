using System;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Data for a guitar packet.
    /// </summary>
    public struct GuitarPacket
    {
        public uint InstrumentID;
        public string InstrumentIDString;

        public bool MenuButton;
        public bool OptionsButton;
        public bool XboxButton;

        public bool DpadUp;
        public bool DpadDown;
        public bool DpadLeft;
        public bool DpadRight;

        public bool UpperGreen;
        public bool UpperRed;
        public bool UpperYellow;
        public bool UpperBlue;
        public bool UpperOrange;

        public bool LowerGreen;
        public bool LowerRed;
        public bool LowerYellow;
        public bool LowerBlue;
        public bool LowerOrange;

        public byte PickupSwitch;
        public byte WhammyBar;
        public byte Tilt;
    }

    /// <summary>
    /// Functionality to analyze guitar packets into a GuitarPacket struct.
    /// </summary>
    public class GuitarPacketReader
    {
        /// <summary>
        /// Packet definitions for the frets.
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
        /// Packet definitions for the buttons.
        /// </summary>
        [Flags]
        public enum Buttons : byte
        {
            Xbox = 0x01,
            Menu = 0x04,
            Options = 0x08,
        }

        /// <summary>
        /// Packet definitions for the d-pad.
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
        /// Size of guitar packets.
        /// </summary>
        private const int GuitarPacketLength = 40;

        /// <summary>
        /// Size of the packet header.
        /// </summary>
        private const int XboxHeaderLength = 22;

        /// <summary>
        /// Position in the packet from the header.
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
        /// Analyzes a packet and assigns its data to a GuitarPacket struct.
        /// </summary>
        /// <param name="packet">The data packet to use.</param>
        /// <param name="data">A returned GuitarPacket.</param>
        /// <returns>True if packet was used and analyzed, false otherwise.</returns>
        public static bool AnalyzePacket(byte[] packet, ref GuitarPacket data)
        {
            // Check packet
            if (packet != null && packet.Length == GuitarPacketLength)
            {
                // Assign instrument ID
                // String representation: AA BB CC DD
                data.InstrumentID = (uint)(
                    packet[15] |         // DD
                    (packet[14] << 8) |  // CC
                    (packet[13] << 16) | // BB
                    (packet[12] << 24)   // AA
                );

                // Map buttons
                byte buttons = packet[XboxHeaderLength + (int)PacketPosition.Buttons];

                // Menu
                data.MenuButton = (buttons & (byte)Buttons.Menu) != 0;
                
                // Options
                data.OptionsButton = (buttons & (byte)Buttons.Options) != 0;
                
                // Xbox
                data.XboxButton = (buttons & (byte)Buttons.Xbox) != 0;

                // Map Dpad
                byte dpad = packet[XboxHeaderLength + (int)PacketPosition.Dpad];

                // Dpad Up
                data.DpadUp = (dpad & (byte)Dpad.Up) != 0;
                
                // Dpad Down
                data.DpadDown = (dpad & (byte)Dpad.Down) != 0;
                
                // Dpad Left
                data.DpadLeft = (dpad & (byte)Dpad.Left) != 0;
                
                // Dpad Right
                data.DpadRight = (dpad & (byte)Dpad.Right) != 0;

                // Frets
                byte upperFret = packet[XboxHeaderLength + (int)PacketPosition.UpperFret];
                byte lowerFret = packet[XboxHeaderLength + (int)PacketPosition.LowerFret];

                // Fret Green
                data.UpperGreen = (upperFret & (byte)Frets.Green) != 0;
                data.LowerGreen = (lowerFret & (byte)Frets.Green) != 0;
                
                // Fret Red
                data.UpperRed = (upperFret & (byte)Frets.Red) != 0;
                data.LowerRed = (lowerFret & (byte)Frets.Red) != 0;
                
                // Fret Yellow
                data.UpperYellow = (upperFret & (byte)Frets.Yellow) != 0;
                data.LowerYellow = (lowerFret & (byte)Frets.Yellow) != 0;
                
                // Fret Blue
                data.UpperBlue = (upperFret & (byte)Frets.Blue) != 0;
                data.LowerBlue = (lowerFret & (byte)Frets.Blue) != 0;
                
                // Fret Orange
                data.UpperOrange = (upperFret & (byte)Frets.Orange) != 0;
                data.LowerOrange = (lowerFret & (byte)Frets.Orange) != 0;

                // Pickup Switch
                data.PickupSwitch = packet[XboxHeaderLength + (int)PacketPosition.Slider];

                // Whammy Bar
                data.WhammyBar = packet[XboxHeaderLength + (int)PacketPosition.Whammy];

                // Tilt
                data.Tilt = packet[XboxHeaderLength + (int)PacketPosition.Tilt];

                // Packet handled
                return true;
            }

            // Packet ignored
            return false;
        }
    }
}
