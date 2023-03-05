using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Header data for a message.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CommandHeader
    {
        /// <summary>
        /// Command ID definitions.
        /// </summary>
        public enum Command : byte
        {
            Input = 0x20
        }

        /// <summary>
        /// Flag definitions.
        /// </summary>
        [Flags]
        public enum Flags : byte
        {
            None = 0,
            NeedsAcknowledgement = 0x10,
            SystemCommand = 0x20,
            ChunkStart = 0x40,
            ChunkPacket = 0x80
        }

        public Command CommandId;
        public Flags CommandFlags;
        public byte SequenceCount;
        public int DataLength;

        public static bool TryParse(ReadOnlySpan<byte> data, out CommandHeader header, out int bytesRead)
        {
            header = default;
            bytesRead = 0;
            if (data == null || data.Length < 4)
            {
                return false;
            }

            if (!ParsingUtils.DecodeLEB128(data.Slice(3), out int dataLength, out int byteLength))
            {
                return false;
            }

            header = new CommandHeader()
            {
                CommandId = (Command)data[0],
                CommandFlags = (Flags)data[1],
                SequenceCount = data[2],
                DataLength = dataLength
            };
            bytesRead = 3 + byteLength;

            return true;
        }
    }

    /// <summary>
    /// Flag definitions for the buttons bytes.
    /// </summary>
    [Flags]
    enum GamepadButton : ushort
    {
        Sync = 0x0001,
        Unused = 0x0002,
        Menu = 0x0004,
        Options = 0x0008,
        A = 0x0010,
        B = 0x0020,
        X = 0x0040,
        Y = 0x0080,
        DpadUp = 0x0100,
        DpadDown = 0x0200,
        DpadLeft = 0x0400,
        DpadRight = 0x0800,
        LeftBumper = 0x1000,
        RightBumper = 0x2000,
        LeftStickPress = 0x4000,
        RightStickPress = 0x8000
    }

    /// <summary>
    /// An input report from a guitar.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct GuitarInput
    {
        /// <summary>
        /// Re-definitions for button flags that have specific meanings.
        /// </summary>
        [Flags]
        public enum Button : ushort
        {
            StrumUp = GamepadButton.DpadUp,
            StrumDown = GamepadButton.DpadDown,
            GreenFret = GamepadButton.A,
            RedFret = GamepadButton.B,
            YellowFret = GamepadButton.Y,
            BlueFret = GamepadButton.X,
            OrangeFret = GamepadButton.LeftBumper,
            LowerFretFlag = GamepadButton.LeftStickPress
        }

        /// <summary>
        /// Flags used in <see cref="UpperFrets"/> and <see cref="LowerFrets"/>
        /// </summary>
        [Flags]
        public enum Fret : byte
        {
            Green = 0x01,
            Red = 0x02,
            Yellow = 0x04,
            Blue = 0x08,
            Orange = 0x10
        }

        public ushort Buttons;
        public byte Tilt;
        public byte WhammyBar;
        public byte PickupSwitch;
        public byte UpperFrets;
        public byte LowerFrets;
        byte unk1;
        byte unk2;
        byte unk3;

        public bool Green => ((UpperFrets | LowerFrets) & (byte)Fret.Green) != 0;
        public bool Red => ((UpperFrets | LowerFrets) & (byte)Fret.Red) != 0;
        public bool Yellow => ((UpperFrets | LowerFrets) & (byte)Fret.Yellow) != 0;
        public bool Blue => ((UpperFrets | LowerFrets) & (byte)Fret.Blue) != 0;
        public bool Orange => ((UpperFrets | LowerFrets) & (byte)Fret.Orange) != 0;

        public bool LowerFretFlag => (Buttons & (ushort)Button.LowerFretFlag) != 0;
    }

    /// <summary>
    /// An input report from a drumkit.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DrumInput
    {
        /// <summary>
        /// Re-definitions for button flags that have specific meanings.
        /// </summary>
        [Flags]
        public enum Button : ushort
        {
            // Not used as these are for menu navigation purposes
            // RedPad = GamepadButton.B,
            // GreenPad = GamepadButton.A,
            KickOne = GamepadButton.LeftBumper,
            KickTwo = GamepadButton.RightBumper
        }

        /// <summary>
        /// Masks for each pad's value.
        /// </summary>
        enum Pad : ushort
        {
            Red = 0x00F0,
            Yellow = 0x000F,
            Blue = 0xF000,
            Green = 0x0F00
        }

        /// <summary>
        /// Masks for each cymbal's value.
        /// </summary>
        enum Cymbal : ushort
        {
            Yellow = 0x00F0,
            Blue = 0x000F,
            Green = 0xF000
        }

        public ushort Buttons;
        ushort pads;
        ushort cymbals;

        public byte RedPad => getByteValue(pads, (ushort)Pad.Red, 4);
        public byte YellowPad => getByteValue(pads, (ushort)Pad.Yellow, 0);
        public byte BluePad => getByteValue(pads, (ushort)Pad.Blue, 12);
        public byte GreenPad => getByteValue(pads, (ushort)Pad.Green, 8);

        public byte YellowCymbal => getByteValue(cymbals, (ushort)Cymbal.Yellow, 4);
        public byte BlueCymbal => getByteValue(cymbals, (ushort)Cymbal.Blue, 0);
        public byte GreenCymbal => getByteValue(cymbals, (ushort)Cymbal.Green, 12);

        /// <summary>
        /// Gets a byte value from a ushort field of multiple values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte getByteValue(ushort field, ushort mask, ushort offset)
        {
            return (byte)((field & mask) >> offset);
        }
    }
}