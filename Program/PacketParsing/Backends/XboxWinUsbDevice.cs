using System;
using System.Diagnostics;
using System.Threading;
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

        private Thread readThread;
        private volatile bool readPackets = false;

        private XboxWinUsbDevice(USBDevice usb, USBInterface @interface)
            : base(@interface.OutPipe.MaximumPacketSize)
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

        public void StartReading()
        {
            if (readPackets)
                return;

            readPackets = true;
            readThread = new Thread(ReadThread);
            readThread.Start();
        }

        public void StopReading()
        {
            if (!readPackets)
                return;

            readPackets = false;
            mainInterface.InPipe.Abort();
            readThread.Join();
            readThread = null;
        }

        private void ReadThread()
        {
            Span<byte> readBuffer = stackalloc byte[mainInterface.InPipe.MaximumPacketSize];

            // Number of errors after which reading will stop
            const int errorThreshold = 3;
            int errorCount = 0;
            while (readPackets)
            {
                // Read packet data
                int bytesRead = ReadPacket(readBuffer);
                if (bytesRead < 0)
                {
                    if (errorCount > errorThreshold)
                        break;

                    errorCount++;
                    continue;
                }

                // Process packet data
                Debug.WriteLine(ParsingUtils.ToString(readBuffer));
                var result = HandlePacket(readBuffer.Slice(0, bytesRead));
                switch (result)
                {
                    case XboxResult.InvalidMessage:
                        Debug.WriteLine($"Invalid packet received!");
                        break;
                }
            }
        }

        private int ReadPacket(Span<byte> readBuffer)
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

        protected override XboxResult SendPacket(Span<byte> data)
        {
            try
            {
                mainInterface.InPipe.Write(data);
                return XboxResult.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while sending packet: {ex}");
                return XboxResult.InvalidMessage;
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

            StopReading();
            usbDevice?.Dispose();
            usbDevice = null;
            mainInterface = null;
        }
    }
}