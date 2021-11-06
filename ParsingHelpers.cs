using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RB4InstrumentMapper
{
    public static class ParsingHelpers
    {
        /// <summary>
        /// Convert an 32bit integer represented as a hex string into a byte array.
        /// </summary>
        /// <param name="hexString">32bit integer hex string to convert.</param>
        /// <returns>Byte array representing the integer hex string, or null if conversion failed.</returns>
        public static byte[] Int32HexStringToByteArray(string hexString)
        {
            byte[] byteArray = null;
            uint number;
            if (uint.TryParse(hexString, System.Globalization.NumberStyles.AllowHexSpecifier, null, out number))
            {
                byteArray = BitConverter.GetBytes(number);
            }

            return byteArray;
        }

        /// <summary>
        /// Convert a byte array into a hex string.
        /// </summary>
        /// <param name="byteArray">Byte array to convert</param>
        /// <returns>Hex string representing the byte array, or null if input is null or empty.</returns>
        public static string ByteArrayToHexString(byte[] byteArray)
        {
            string hexString = null;
            if (byteArray != null && byteArray.Length > 0)
            {
                hexString = BitConverter.ToString(byteArray).Replace("-", string.Empty);
            }

            return hexString;
        }
    }
}
