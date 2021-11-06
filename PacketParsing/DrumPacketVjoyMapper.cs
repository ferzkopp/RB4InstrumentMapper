using System;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Maps RockBand 4 Drum packets from WinPcap to vJoy virtual HID.
    /// </summary>
    public class DrumPacketVjoyMapper
    {
        /// <summary>
        /// Packet definitions for Drums
        /// </summary>
        [Flags]
        public enum Drums : byte
        {
            Red = 0x20,

            Yellow1 = 0x01,
            Yellow2 = 0x02,
            Yellow3 = 0x03,
            Yellow4 = 0x04,
            Yellow5 = 0x05,
            Yellow6 = 0x06,
            Yellow7 = 0x07,

            Blue1 = 0x10,
            Blue2 = 0x20,
            Blue3 = 0x30,
            Blue4 = 0x40,
            Blue5 = 0x50,
            Blue6 = 0x60,

            Green = 0x10,

            BassOne = 0x10,
            BassTwo = 0x20,
        }

        /// <summary>
        /// Packet definitions for Cymbals
        /// </summary>

        [Flags]
        public enum Cymbals : byte
        {
            	Yellow1 = 0x10,
            	Yellow2 = 0x20,
            	Yellow3 = 0x30,
            	Yellow4 = 0x40,
            	Yellow5 = 0x50,
            	Yellow6 = 0x60,
            	Yellow7 = 0x70,

            	Blue1 = 0x1,
            	Blue2 = 0x2,
            	Blue3 = 0x3,
            	Blue4 = 0x4,
            	Blue5 = 0x5,
            	Blue6 = 0x6,
            	Blue7 = 0x7,

            	Green1 = 0x10,
            	Green2 = 0x20,
            	Green3 = 0x30,
            	Green4 = 0x40,
            	Green5 = 0x50,
            	Green6 = 0x60,
            	Green7 = 0x70,
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
            Up = 0x01,
            Down = 0x02,
            Left = 0x04,
            Right = 0x08,
        }

        /// <summary>
        /// Size of packet header
        /// </summary>
        private const int XboxHeaderLength = 22;

        /// <summary>
        /// Position in the packet from header
        /// </summary>
        public enum PacketPosition : int
        {
            RedDrum = 8,
            YellowDrum = 10,
            BlueDrum = 11,
            GreenDrum = 8,
            BassPedal = 9,
            YellowCymbal = 12,
            BlueCymbal = 12,
            GreenCymbal = 13,
            Buttons = 8,
            Dpad = 9,
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
            if (packet != null && packet.Length == 36)
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

                // Map drums

                // Red drum
                byte redDrum = packet[XboxHeaderLength + (int)PacketPosition.RedDrum];
                bool redDrumButton = (redDrum & (byte)Drums.Red) != 0;
                if (redDrumButton)
                {
                    iReport.Buttons |= (uint)1 << 1;
                }

                // Yellow drum
                byte yellowDrum = packet[XboxHeaderLength + (int)PacketPosition.YellowDrum];
                bool yellowDrumButton =
                    (yellowDrum & (byte)Drums.Yellow1) != 0 ||
                    (yellowDrum & (byte)Drums.Yellow2) != 0 ||
                    (yellowDrum & (byte)Drums.Yellow3) != 0 ||
                    (yellowDrum & (byte)Drums.Yellow4) != 0 ||
                    (yellowDrum & (byte)Drums.Yellow5) != 0 ||
                    (yellowDrum & (byte)Drums.Yellow6) != 0 ||
                    (yellowDrum & (byte)Drums.Yellow7) != 0;
                if (yellowDrumButton)
                {
                    iReport.Buttons |= (uint)1 << 2;
                }

                // Blue drum
                byte blueDrum = packet[XboxHeaderLength + (int)PacketPosition.BlueDrum];
                bool blueDrumButton =
                    (blueDrum & (byte)Drums.Blue1) != 0 ||
                    (blueDrum & (byte)Drums.Blue2) != 0 ||
                    (blueDrum & (byte)Drums.Blue3) != 0 ||
                    (blueDrum & (byte)Drums.Blue4) != 0 ||
                    (blueDrum & (byte)Drums.Blue5) != 0 ||
                    (blueDrum & (byte)Drums.Blue6) != 0;
                if (blueDrumButton)
                {
                    iReport.Buttons |= (uint)1 << 3;
                }

                // Green drum
                byte greenDrum = packet[XboxHeaderLength + (int)PacketPosition.GreenDrum];
                bool greenDrumButton =
                    (greenDrum & (byte)Drums.Green) != 0;
                if (greenDrumButton)
                {
                    iReport.Buttons |= (uint)1 << 4;
                }

                // Base drums
                byte baseDrum = packet[XboxHeaderLength + (int)PacketPosition.BassPedal];
                bool bassOneButton =
                    (baseDrum & (byte)Drums.BassOne) != 0;
                if (bassOneButton)
                {
                    iReport.Buttons |= (uint)1 << 5;
                }

                bool baseTwoButton =
                    (baseDrum & (byte)Drums.BassTwo) != 0;
                if (baseTwoButton)
                {
                    iReport.Buttons |= (uint)1 << 6;
                }

                // Map cymbals

                // Yellow cymbal
                byte yellowCymbal = packet[XboxHeaderLength + (int)PacketPosition.YellowCymbal];
                bool yellowCymbalButton =
                    (yellowCymbal & (byte)Cymbals.Yellow1) != 0 ||
                    (yellowCymbal & (byte)Cymbals.Yellow2) != 0 ||
                    (yellowCymbal & (byte)Cymbals.Yellow3) != 0 ||
                    (yellowCymbal & (byte)Cymbals.Yellow4) != 0 ||
                    (yellowCymbal & (byte)Cymbals.Yellow5) != 0 ||
                    (yellowCymbal & (byte)Cymbals.Yellow6) != 0 ||
                    (yellowCymbal & (byte)Cymbals.Yellow7) != 0;
                if (yellowCymbalButton)
                {
                    iReport.Buttons |= (uint)1 << 7;
                }

                // Blue cymbal
                byte blueCymbal = packet[XboxHeaderLength + (int)PacketPosition.BlueCymbal];
                bool blueCymbalButton =
                    (blueCymbal & (byte)Cymbals.Blue1) != 0 ||
                    (blueCymbal & (byte)Cymbals.Blue2) != 0 ||
                    (blueCymbal & (byte)Cymbals.Blue3) != 0 ||
                    (blueCymbal & (byte)Cymbals.Blue4) != 0 ||
                    (blueCymbal & (byte)Cymbals.Blue5) != 0 ||
                    (blueCymbal & (byte)Cymbals.Blue6) != 0 ||
                    (blueCymbal & (byte)Cymbals.Blue7) != 0;
                if (blueCymbalButton)
                {
                    iReport.Buttons |= (uint)1 << 8;
                }

                // Green cymbal
                byte greenCymbal = packet[XboxHeaderLength + (int)PacketPosition.GreenCymbal];
                bool greenCymbalButton =
                    (greenCymbal & (byte)Cymbals.Green1) != 0 ||
                    (greenCymbal & (byte)Cymbals.Green2) != 0 ||
                    (greenCymbal & (byte)Cymbals.Green3) != 0 ||
                    (greenCymbal & (byte)Cymbals.Green4) != 0 ||
                    (greenCymbal & (byte)Cymbals.Green5) != 0 ||
                    (greenCymbal & (byte)Cymbals.Green6) != 0 ||
                    (greenCymbal & (byte)Cymbals.Green7) != 0;
                if (greenCymbalButton)
                {
                    iReport.Buttons |= (uint)1 << 9;
                }

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
