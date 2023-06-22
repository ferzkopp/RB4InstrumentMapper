using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Common interface for device mappers.
    /// </summary>
    internal interface IDeviceMapper : IDisposable
    {
        bool MapGuideButton { get; set; }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        XboxResult HandlePacket(CommandId command, ReadOnlySpan<byte> data);
    }
}