using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace RB4InstrumentMapper.Parsing
{
    public static class WinUsbBackend
    {
        private static readonly DeviceNotificationListener watcher = new DeviceNotificationListener();
        private static readonly Dictionary<string, XboxWinUsbDevice> devices = new Dictionary<string, XboxWinUsbDevice>();

        public static int DeviceCount => devices.Count;

        public static event Action DeviceAddedOrRemoved;

        private static bool started = false;

        public static void Initialize()
        {
            foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
            {
                AddDevice(deviceInfo.DevicePath);
            }

            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);
        }

        public static void Uninitialize()
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

        public static void Start()
        {
            if (started)
                return;

            foreach (var device in devices.Values)
            {
                device.StartReading();
            }

            started = true;
        }

        public static void Stop()
        {
            if (!started)
                return;

            foreach (var device in devices.Values)
            {
                device.StopReading();
            }

            started = false;
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
            // Paths are case-insensitive
            devicePath = devicePath.ToLowerInvariant();
            var device = XboxWinUsbDevice.TryCreate(devicePath);
            if (device == null)
                return;

            devices.Add(devicePath, device);
            if (started)
                device.StartReading();

            Debug.WriteLine($"Added device {devicePath}");
            DeviceAddedOrRemoved?.Invoke();
        }

        private static void RemoveDevice(string devicePath)
        {
            // Paths are case-insensitive
            devicePath = devicePath.ToLowerInvariant();
            if (!devices.TryGetValue(devicePath, out var device))
                return;

            devices.Remove(devicePath);
            device.Dispose();

            Debug.WriteLine($"Removed device {devicePath}");
            DeviceAddedOrRemoved?.Invoke();
        }
    }
}