using System;
using System.Diagnostics;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace RB4InstrumentMapper.Parsing
{
    internal class XboxWinUsbDevice : XboxDevice
    {
        private static readonly Guid WinUsbClassGuid = Guid.Parse("88BAE032-5A81-49F0-BC3D-A4FF138216D6");
        private const string XGIP_COMPATIBLE_ID = @"USB\MS_COMP_XGIP10";
        private const byte XBOX_INTERFACE_CLASS = 0xFF; // Vendor-specific
        private const byte XBOX_INTERFACE_SUB_CLASS = 0x47;
        private const byte XBOX_INTERFACE_PROTOCOL = 0xD0;

        private USBDevice usbDevice;
        private USBInterface mainInterface;

        public int InputSize => mainInterface.InPipe.MaximumPacketSize;

        private XboxWinUsbDevice(USBDevice usb, USBInterface @interface)
        {
            usbDevice = usb;
            mainInterface = @interface;
        }

        public static XboxWinUsbDevice TryCreate(string devicePath)
        {
            // Only accept WinUSB devices, at least for now
            var pnpDevice = PnPDevice.GetDeviceByInterfaceId(devicePath);
            var classGuid = pnpDevice.GetProperty<Guid>(DevicePropertyKey.Device_ClassGuid);
            if (classGuid != WinUsbClassGuid)
                return null;

            // Check for the Xbox One compatible ID
            if (!HasCompatibleId(pnpDevice, XGIP_COMPATIBLE_ID))
                return null;

            // Open device
            var usbDevice = USBDevice.GetSingleDeviceByPath(devicePath);

            // Get input data pipe
            var mainInterface = FindMainInterface(usbDevice);
            if (mainInterface == null)
            {
                usbDevice.Dispose();
                return null;
            }

            return new XboxWinUsbDevice(usbDevice, mainInterface);
        }

        public int ReadPacket(Span<byte> readBuffer)
        {
            try
            {
                return mainInterface.InPipe.Read(readBuffer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while reading packet: {ex}");
                return -1;
            }
        }

        private static bool HasCompatibleId(PnPDevice pnpDevice, string compatibleId)
        {
            var compatibleIds = pnpDevice.GetProperty<string[]>(DevicePropertyKey.Device_CompatibleIds);
            foreach (string id in compatibleIds)
            {
                if (id == compatibleId)
                    return true;
            }

            return false;
        }

        private static USBInterface FindMainInterface(USBDevice device)
        {
            foreach (var iface in device.Interfaces)
            {
                // Ignore non-XGIP interfaces
                if (iface.ClassValue != XBOX_INTERFACE_CLASS ||
                    iface.SubClass != XBOX_INTERFACE_SUB_CLASS ||
                    iface.Protocol != XBOX_INTERFACE_PROTOCOL)
                    continue;

                // The main interface uses interrupt transfers
                if (iface.InPipe?.TransferType != USBTransferType.Interrupt ||
                    iface.OutPipe?.TransferType != USBTransferType.Interrupt)
                    continue;

                return iface;
            }

            return null;
        }

        protected override void ReleaseManagedResources()
        {
            base.ReleaseManagedResources();

            usbDevice?.Dispose();
            usbDevice = null;
            mainInterface = null;
        }
    }
}