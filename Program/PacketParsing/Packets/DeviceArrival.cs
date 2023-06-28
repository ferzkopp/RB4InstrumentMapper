using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct DeviceArrival
    {
        public readonly ulong SerialNumber;
        public readonly ushort VendorId;
        public readonly ushort ProductId;
        private readonly ulong ignored1;
        private readonly ulong ignored2;
    }
}