using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

// TODO: Doesn't actually work yet, and will block if a device hasn't sent any inputs yet

namespace RB4InstrumentMapper.Parsing
{
    public static class WinUsbBackend
    {
        private static readonly Thread readThread = new Thread(ReadThread) { IsBackground = true };
        private static volatile bool stopReading = false;

        private static readonly DeviceNotificationListener watcher = new DeviceNotificationListener();
        private static readonly ConcurrentDictionary<string, XboxWinUsbDevice> devices
            = new ConcurrentDictionary<string, XboxWinUsbDevice>();

        public static void Start()
        {
            foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
            {
                AddDevice(deviceInfo.DevicePath);
            }

            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);

            readThread.Start();
        }

        public static void Stop()
        {
            stopReading = true;
            readThread.Join();

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

            devices.TryAdd(devicePath, device);
        }

        private static void RemoveDevice(string devicePath)
        {
            if (devices.TryRemove(devicePath, out var device))
            {
                device.Dispose();
            }
        }

        private static void ReadThread()
        {
            while (!stopReading)
            {
                foreach (var device in devices.Values)
                {
                    if (stopReading)
                        break;

                    // Read continuously while a chunk sequence is going on
                    XboxResult result;
                    do
                    {
                        result = ReadPacket(device);
                    }
                    while (result == XboxResult.Pending);
                }
            }
        }

        private static XboxResult ReadPacket(XboxWinUsbDevice device)
        {
            Span<byte> readBuffer = stackalloc byte[device.InputSize];
            int bytesRead = device.ReadPacket(readBuffer);
            if (bytesRead < 0)
                return XboxResult.InvalidMessage;

            Debug.WriteLine(ParsingUtils.ToString(readBuffer));
            var result = device.HandlePacket(readBuffer.Slice(0, bytesRead));
            switch (result)
            {
                case XboxResult.InvalidMessage:
                    Debug.WriteLine($"Invalid packet received!");
                    break;
            }
            return result;
        }
    }
}