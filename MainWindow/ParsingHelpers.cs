using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RB4InstrumentMapper
{
    public static class ParsingHelpers
    {
        /// <summary>
        /// Converts a hex string representing a 32-bit integer into a byte array.
        /// </summary>
        /// <param name="hexString">The string to be converted.</param>
        /// <returns>A byte array converted from the hex string, or null if the conversion failed.</returns>
        public static byte[] Int32HexStringToByteArray(string hexString)
        {
            byte[] byteArray = null;
            uint number;
            if (uint.TryParse(hexString, NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out number))
            {
                byteArray = BitConverter.GetBytes(number);
            }

            return byteArray;
        }

        /// <summary>
        /// Converts a byte array into a hex string.
        /// </summary>
        /// <param name="byteArray">The byte array to converted.</param>
        /// <returns>A hex string representing the byte array, or null if input is null or empty.</returns>
        public static string ByteArrayToHexString(byte[] byteArray)
        {
            string hexString = null;
            if (byteArray != null && byteArray.Length > 0)
            {
                hexString = BitConverter.ToString(byteArray).Replace("-", string.Empty);
            }

            return hexString;
        }

        /// <summary>
        /// Converts a string representing a 32-bit hexadecimal number into a 32-bit unsigned integer.
        /// </summary>
        /// <param name="hexString">The string to be converted.</param>
        /// <param name="number">The converted number.</param>
        /// <returns>True if the conversion was successful, or false if it failed.</returns>
        public static bool HexStringToUInt32(string hexString, out uint number)
        {
            if (hexString.StartsWith("0x") || hexString.StartsWith("&h"))
            {
                hexString = hexString.Remove(0, 2);
            }

            return uint.TryParse(hexString, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out number);
        }

        /// <summary>
        /// Converts a 32-bit unsigned integer into a string representing a 32-bit hexadecimal number.
        /// </summary>
        /// <param name="number">The number to be converted.</param>
        /// <param name="isID">A flag indicating if this is an instrument ID.</param>
        /// <returns>A string representing the input number, or String.Empty if the input is 0 and isID is set.</returns>
        public static string UInt32ToHexString(uint number, bool isID)
        {
            if (isID)
            {
                return number == 0 ? String.Empty : Convert.ToString(number, 16);
            }
            else
            {
                return Convert.ToString(number, 16);
            }
        }
    }
}
