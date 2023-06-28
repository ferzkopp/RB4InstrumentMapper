using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Helper functions for parsing.
    /// </summary>
    internal static class ParsingUtils
    {
        public static string ToString(ReadOnlySpan<byte> buffer)
        {
            const string characters = "0123456789ABCDEF";

            if (buffer.IsEmpty)
                return "";

            Span<char> stringBuffer = stackalloc char[buffer.Length * 3];
            for (int i = 0; i < buffer.Length; i++)
            {
                byte value = buffer[i];
                int stringIndex = i * 3;
                stringBuffer[stringIndex] = characters[(value & 0xF0) >> 4];
                stringBuffer[stringIndex + 1] = characters[value & 0x0F];
                stringBuffer[stringIndex + 2] = '-';
            }
            // Exclude last '-'
            stringBuffer = stringBuffer.Slice(0, stringBuffer.Length - 1);

            return stringBuffer.ToString();
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
