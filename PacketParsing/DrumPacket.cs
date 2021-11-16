using System;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Data for a drumkit packet.
    /// </summary>
    public struct DrumPacket
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

        public bool RedDrum;
        public bool YellowDrum;
        public bool BlueDrum;
        public bool GreenDrum;

        public bool YellowCymbal;
        public bool BlueCymbal;
        public bool GreenCymbal;

        public bool BassOne;
        public bool BassTwo;
    }

    /// <summary>
    /// Functionality to analyze drumkit packets into a DrumPacket struct.
    /// </summary>
    public class DrumPacketReader
    {
        /// <summary>
        /// Packet definitions for the pads/cymbals.
        /// </summary>
        public enum Drums : byte
        {
            RedDrum = 0x20,
            YellowDrum = 0x0F,
            BlueDrum = 0xF0,
            GreenDrum = 0x10,

            YellowCymbal = 0xF0,
            BlueCymbal = 0x0F,
            GreenCymbal = 0xF0,

            BassOne = 0x10,
            BassTwo = 0x20,
        }

        /// <summary>
        /// Packet definitions for the face buttons.
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
            Up = 0x01,
            Down = 0x02,
            Left = 0x04,
            Right = 0x08,
        }

        /// <summary>
        /// Size of drumkit packets.
        /// </summary>
        private const int DrumPacketLength = 36;

        /// <summary>
        /// Size of the packet header.
        /// </summary>
        private const int XboxHeaderLength = 22;

        /// <summary>
        /// Position in the packet from the header.
        /// </summary>
        public enum PacketPosition : int
        {
            RedGreenDrum = 8,
            YellowDrum = 10,
            BlueDrum = 11,
            YellowBlueCymbal = 12,
            GreenCymbal = 13,
            BassPedal = 9,
            Buttons = 8,
            Dpad = 9,
        }

        /// <summary>
        /// Analyzes a packet and assigns its data to a DrumPacket struct.
        /// </summary>
        /// <param name="packet">The data packet to be analyzed.</param>
        /// <param name="data">A returned DrumPacket.</param>
        /// <returns>True if packet was used and analyzed, false otherwise.</returns>
        public static bool AnalyzePacket(byte[] packet, out DrumPacket data)
        {
            data = new DrumPacket();
            if (packet != null && packet.Length == DrumPacketLength)
            {
                // Assign instrument ID
                // String representation: AA BB CC DD
                data.InstrumentID = (uint)(
                    packet[15] |         // DD
                    (packet[14] << 8) |  // CC
                    (packet[13] << 16) | // BB
                    (packet[12] << 24)   // AA
                );
                data.InstrumentIDString = ParsingHelpers.UInt32ToHexString(data.InstrumentID, isID: true);

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

                // Map drums
                byte redGreenDrum = packet[XboxHeaderLength + (int)PacketPosition.RedGreenDrum];
                byte yellowDrum = packet[XboxHeaderLength + (int)PacketPosition.YellowDrum];
                byte blueDrum = packet[XboxHeaderLength + (int)PacketPosition.BlueDrum];
                byte bassDrum = packet[XboxHeaderLength + (int)PacketPosition.BassPedal];

                    // Red drum
                    data.RedDrum = (redGreenDrum & (byte)Drums.RedDrum) != 0;
                    // Yellow drum
                    data.YellowDrum = (yellowDrum & (byte)Drums.YellowDrum) != 0;
                    // Blue drum
                    data.BlueDrum = (blueDrum & (byte)Drums.BlueDrum) != 0;
                    // Green drum
                    data.GreenDrum = (redGreenDrum & (byte)Drums.GreenDrum) != 0;

                    // Bass drums
                    data.BassOne = (bassDrum & (byte)Drums.BassOne) != 0;
                    data.BassTwo = (bassDrum & (byte)Drums.BassTwo) != 0;

                // Map cymbals
                byte yellowBlueCymbal = packet[XboxHeaderLength + (int)PacketPosition.YellowBlueCymbal];
                byte greenCymbal = packet[XboxHeaderLength + (int)PacketPosition.GreenCymbal];

                    // Yellow cymbal
                    data.YellowCymbal = (yellowBlueCymbal & (byte)Drums.YellowCymbal) != 0;
                    // Blue cymbal
                    data.BlueCymbal = (yellowBlueCymbal & (byte)Drums.BlueCymbal) != 0;
                    // Green cymbal
                    data.GreenCymbal = (greenCymbal & (byte)Drums.GreenCymbal) != 0;

                // Packet handled
                return true;
            }

            // Packet ignored
            return false;
        }
    }
}
