using System.Collections.Generic;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

// TODO: Doesn't actually work yet, need to send data back to the device

namespace RB4InstrumentMapper.Parsing
{
    public static class WinUsbBackend
    {
        private static readonly DeviceNotificationListener watcher = new DeviceNotificationListener();
        private static readonly Dictionary<string, XboxWinUsbDevice> devices
            = new Dictionary<string, XboxWinUsbDevice>();

        public static void Start()
        {
            foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
            {
                AddDevice(deviceInfo.DevicePath);
            }

            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);
        }

        public static void Stop()
        {
            watcher.StopListen();
            watcher.DeviceArrived -= DeviceArrived;
            watcher.DeviceRemoved -= DeviceRemoved;

            foreach (var device in devices.Values)
            {
                device.Dispose();
            }
            devices.Clear();
        }

        private static void DeviceArrived(DeviceEventArgs args)
        {
            AddDevice(args.SymLink);
        }

        private static void DeviceRemoved(DeviceEventArgs args)
        {
            RemoveDevice(args.SymLink);
        }

        private static void AddDevice(string devicePath)
        {
            var device = XboxWinUsbDevice.TryCreate(devicePath);
            if (device == null)
                return;

            devices.Add(devicePath, device);
        }

        private static void RemoveDevice(string devicePath)
        {
            if (!devices.TryGetValue(devicePath, out var device))
                return;

            devices.Remove(devicePath);
            device.Dispose();
        }
    }
}