using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Common interface for device mappers.
    /// </summary>
    interface IDeviceMapper : IDisposable
    {
        /// <summary>
        /// Parses an input packet.
        /// </summary>
        void ParseInput(CommandHeader header, ReadOnlySpan<byte> data);
    }
}