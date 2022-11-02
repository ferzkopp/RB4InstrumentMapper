using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Common interface for device mappers.
    /// </summary>
    interface IDeviceMapper
    {
        /// <summary>
        /// Parses an input packet.
        /// </summary>
        void ParseInput(CommandHeader header, ReadOnlySpan<byte> data);

        /// <summary>
        /// Performs cleanup for the mapper.
        /// </summary>
        void Close();
    }
}