using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace RB4InstrumentMapper.Parsing
{
    public static class WinUsbBackend
    {
        private static readonly DeviceNotificationListener watcher = new DeviceNotificationListener();
        private static readonly ConcurrentDictionary<string, XboxWinUsbDevice> devices = new ConcurrentDictionary<string, XboxWinUsbDevice>();

        public static int DeviceCount => devices.Count;

        public static event Action DeviceAddedOrRemoved;

        public static bool Initialized { get; private set; } = false;
        public static bool Started { get; private set; } = false;

        public static void Initialize()
        {
            if (Initialized)
                return;

            foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
            {
                AddDevice(deviceInfo.DevicePath);
            }

            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);

            Initialized = true;
        }

        public static void Uninitialize()
        {
            if (!Initialized)
                return;

            watcher.StopListen();
            watcher.DeviceArrived -= DeviceArrived;
            watcher.DeviceRemoved -= DeviceRemoved;

            foreach (var devicePath in devices.Keys)
            {
                RemoveDevice(devicePath, remove: false);
            }
            devices.Clear();

            Initialized = false;
        }

        public static void Start()
        {
            if (Started)
                return;

            foreach (var pair in devices)
            {
                string path = pair.Key;
                var device = pair.Value;

                Debug.Assert(device == null, "Initialized device found!");
                InitializeDevice(path);
            }

            Started = true;
        }

        public static void Stop()
        {
            if (!Started)
                return;

            foreach (var pair in devices)
            {
                string path = pair.Key;
                var device = pair.Value;

                device.StopReading();
                devices[path] = null;
            }

            Started = false;
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
            if (!XboxWinUsbDevice.IsCompatibleDevice(devicePath))
                return;

            if (Started)
                InitializeDevice(devicePath);
            else
                devices.TryAdd(devicePath, null);

            PacketLogging.PrintMessage($"Added device {devicePath}");
            DeviceAddedOrRemoved?.Invoke();
        }

        private static void InitializeDevice(string path)
        {
            var device = XboxWinUsbDevice.TryCreate(path);
            if (device == null)
            {
                Debug.Fail($"Non-compatible device {path} added to device list!");
                return;
            }

            device.StartReading();
            devices[path] = device;
        }

        private static void RemoveDevice(string devicePath, bool remove = true)
        {
            // Paths are case-insensitive
            devicePath = devicePath.ToLowerInvariant();
            if (!devices.TryGetValue(devicePath, out var device))
                return;

            device?.Dispose();
            if (remove)
                devices.TryRemove(devicePath, out _);

            PacketLogging.PrintMessage($"Removed device {devicePath}");
            DeviceAddedOrRemoved?.Invoke();
        }
    }
}