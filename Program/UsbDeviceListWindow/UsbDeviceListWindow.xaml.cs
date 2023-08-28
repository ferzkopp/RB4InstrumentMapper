using System;
using System.Windows;
using System.Windows.Controls;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;
using RB4InstrumentMapper.Parsing;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Interaction logic for UsbDeviceListWindow.xaml
    /// </summary>
    public partial class UsbDeviceListWindow : Window
    {
        private readonly DeviceNotificationListener watcher = new DeviceNotificationListener();

        public UsbDeviceListWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);

            Refresh();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            watcher.StopListen();
            watcher.Dispose();
        }

        private void Refresh()
        {
            winUsbDeviceList.Children.Clear();
            xgipDeviceList.Children.Clear();

            foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
            {
                AddDevice(deviceInfo.DevicePath);
            }
        }

        private void DeviceArrived(DeviceEventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() => AddDevice(args.SymLink)));
        }

        private void DeviceRemoved(DeviceEventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() => RemoveDevice(args.SymLink)));
        }

        private void AddDevice(string devicePath)
        {
            devicePath = devicePath.ToLower();
            var pnpDevice = PnPDevice.GetDeviceByInterfaceId(devicePath).ToUsbPnPDevice();

            if (XboxWinUsbDevice.IsCompatibleDevice(pnpDevice))
            {
                var deviceControl = new XboxUsbDeviceControl(devicePath, pnpDevice, winusb: true);
                winUsbDeviceList.Children.Add(deviceControl);
            }
            else if (XboxWinUsbDevice.IsXGIPDevice(pnpDevice))
            {
                var deviceControl = new XboxUsbDeviceControl(devicePath, pnpDevice, winusb: false);
                xgipDeviceList.Children.Add(deviceControl);
            }
        }

        private void RemoveDevice(string devicePath)
        {
            devicePath = devicePath.ToLower();

            RemoveDevice(devicePath, winUsbDeviceList);
            RemoveDevice(devicePath, xgipDeviceList);
        }

        private void RemoveDevice(string devicePath, StackPanel devices)
        {
            var children = devices.Children;
            for (int i = 0; i < children.Count; i++)
            {
                var entry = (XboxUsbDeviceControl)children[i];
                if (entry.DevicePath == devicePath)
                {
                    children.Remove(entry);
                    // Continue through everything to ensure removal
                    // For some reason, things don't get removed correctly otherwise
                    i--;
                }
            }

            devices.UpdateLayout();
        }
    }
}
