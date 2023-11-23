using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace RB4InstrumentMapper.Parsing
{
    public static class WinUsbBackend
    {
        private static readonly DeviceNotificationListener watcher = new DeviceNotificationListener();
        private static readonly ConcurrentDictionary<string, XboxWinUsbDevice> devices = new ConcurrentDictionary<string, XboxWinUsbDevice>();

        private static bool inputsEnabled = false;

        public static int DeviceCount => devices.Count;

        public static event Action DeviceAddedOrRemoved;

        public static bool Initialized { get; private set; } = false;

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

            device.EnableInputs(inputsEnabled);
            device.StartReading();
            devices[devicePath] = device;

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
                devices.TryRemove(devicePath, out _);

            PacketLogging.PrintMessage($"Removed device {devicePath}");
            DeviceAddedOrRemoved?.Invoke();
        }

        public static void EnableInputs(bool enabled)
        {
            inputsEnabled = enabled;
            foreach (var device in devices.Values)
            {
                device.EnableInputs(enabled);
            }
        }

        public static bool SwitchDeviceToWinUSB(string instanceId)
        {
            try
            {
                var device = PnPDevice.GetDeviceByInstanceId(instanceId).ToUsbPnPDevice();
                return SwitchDeviceToWinUSB(device);
            }
            catch (Exception ex)
            {
                // Verbose since this will be attempted twice: once in-process, and once in a separate elevated process
                PacketLogging.PrintVerboseException($"Failed to switch device {instanceId} to WinUSB!", ex);
                return false;
            }
        }

        public static bool SwitchDeviceToWinUSB(UsbPnPDevice device)
        {
            try
            {
                if (!XboxWinUsbDevice.IsXGIPDevice(device))
                {
                    Debug.Fail($"Device instance {device.InstanceId} is not an XGIP device!");
                    return false;
                }

                device.InstallNullDriver(out bool reboot);
                if (reboot)
                    device.CyclePort();

                device.InstallCustomDriver("winusb.inf", out reboot);
                if (reboot)
                    device.CyclePort();

                return true;
            }
            catch (Exception ex)
            {
                // Verbose since this will be attempted twice: once in-process, and once in a separate elevated process
                PacketLogging.PrintVerboseException($"Failed to switch device {device.InstanceId} to WinUSB!", ex);
                return false;
            }
        }

        public static bool RevertDevice(string instanceId)
        {
            try
            {
                var device = PnPDevice.GetDeviceByInstanceId(instanceId).ToUsbPnPDevice();
                return RevertDevice(device);
            }
            catch (Exception ex)
            {
                // Verbose since this will be attempted twice: once in-process, and once in a separate elevated process
                PacketLogging.PrintVerboseException($"Failed to revert device {instanceId} to its original driver!", ex);
                return false;
            }
        }

        public static bool RevertDevice(UsbPnPDevice device)
        {
            try
            {
                device.InstallNullDriver(out bool reboot);
                if (reboot)
                    device.CyclePort();

                device.Uninstall(out reboot);
                if (reboot)
                    device.CyclePort();

                return Devcon.Refresh();
            }
            catch (Exception ex)
            {
                // Verbose since this will be attempted twice: once in-process, and once in a separate elevated process
                PacketLogging.PrintVerboseException($"Failed to revert device {device.InstanceId} to its original driver!", ex);
                return false;
            }
        }
    }
}