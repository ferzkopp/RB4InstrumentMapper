using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DeviceArrival
    {
        public ulong SerialNumber;
        public ushort VendorId;
        public ushort ProductId;
        private ulong ignored1;
        private ulong ignored2;
    }
}