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

        private static bool initialized = false;
        private static bool started = false;

        public static void Initialize()
        {
            if (initialized)
                return;

            foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
            {
                AddDevice(deviceInfo.DevicePath);
            }

            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);

            initialized = true;
        }

        public static void Uninitialize()
        {
            if (!initialized)
                return;

            watcher.StopListen();
            watcher.DeviceArrived -= DeviceArrived;
            watcher.DeviceRemoved -= DeviceRemoved;

            foreach (var devicePath in devices.Keys)
            {
                RemoveDevice(devicePath, remove: false);
            }
            devices.Clear();

            initialized = false;
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

            PacketLogging.PrintMessage($"Added device {devicePath}");
            DeviceAddedOrRemoved?.Invoke();
        }

        private static void RemoveDevice(string devicePath, bool remove = true)
        {
            // Paths are case-insensitive
            devicePath = devicePath.ToLowerInvariant();
            if (!devices.TryGetValue(devicePath, out var device))
                return;

            device.Dispose();
            if (remove)
                devices.Remove(devicePath);

            PacketLogging.PrintMessage($"Removed device {devicePath}");
            DeviceAddedOrRemoved?.Invoke();
        }
    }
}