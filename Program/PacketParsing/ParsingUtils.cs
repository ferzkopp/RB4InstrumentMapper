using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Helper functions for parsing.
    /// </summary>
    internal static class ParsingUtils
    {
        // https://en.wikipedia.org/wiki/LEB128
        public static bool DecodeLEB128(ReadOnlySpan<byte> data, out int result, out int byteLength)
        {
            byteLength = 0;
            result = 0;

            if (data == null || data.Length < 1)
            {
                return false;
            }

            // Decode variable-length length value
            // Sequence length is limited to 4 bytes
            byte value = data[0];
            for (int index = 0;
                (index < data.Length) && (index < sizeof(int)) && ((value & 0x80) != 0);
                index++)
            {
                value = data[index];
                result |= (value & 0x7F) << (index * 7);
                byteLength++;
            }

            // Detect length sequences longer than 4 bytes
            if ((value & 0x80) != 0)
            {
                Debug.WriteLine($"Variable-length value is greater than 4 bytes! Buffer: {BitConverter.ToString(data.ToArray())}");
                byteLength = 0;
                result = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Scales this byte to an int, starting from the negative end.
        /// </summary>
        public static int ScaleToInt32(this byte input)
        {
            // Duplicate the input value to the higher 8-bit regions by multiplying by a number with the
            // first bit of each region set to 1, then XOR with the negative bit to make the range start from the negative end
            return (int)((input * 0x01010101) ^ 0x80000000);
        }

        /// <summary>
        /// Scales this byte to a uint.
        /// </summary>
        public static uint ScaleToUInt32(this byte input)
        {
            // Duplicate the input value to the higher 8-bit regions by multiplying by a number with the
            // first bit of each region set to 1
            return (uint)(input * 0x01010101);
        }

        /// <summary>
        /// Scales a byte to a short, starting from the negative end.
        /// </summary>
        public static short ScaleToInt16(this byte input)
        {
            // Duplicate the input value to the higher 8-bit regions by multiplying by a number with the
            // first bit of each region set to 1, then XOR with the negative bit to make the range start from the negative end
            return (short)((input * 0x0101) ^ 0x8000);
        }

        /// <summary>
        /// Scales a byte to an unsigned short.
        /// </summary>
        public static ushort ScaleToUInt16(this byte input)
        {
            // Duplicate the input value to the higher 8-bit regions by multiplying by a number with the
            // first bit of each region set to 1
            return (ushort)(input * 0x0101);
        }
    }
}
