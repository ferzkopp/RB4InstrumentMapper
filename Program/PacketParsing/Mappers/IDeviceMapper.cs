using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Common interface for device mappers.
    /// </summary>
    internal interface IDeviceMapper : IDisposable
    {
        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        void HandlePacket(CommandId command, ReadOnlySpan<byte> data);
    }
}