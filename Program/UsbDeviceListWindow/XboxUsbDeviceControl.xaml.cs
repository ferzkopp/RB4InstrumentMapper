using System;
using System.Windows;
using System.Windows.Controls;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;
using RB4InstrumentMapper.Parsing;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Interaction logic for XboxUsbDeviceControl.xaml
    /// </summary>
    public partial class XboxUsbDeviceControl : UserControl
    {
        public string DevicePath { get; }
        public UsbPnPDevice PnpDevice { get; }
        public bool IsWinUsb { get; }

        public XboxUsbDeviceControl(string devicePath, UsbPnPDevice device, bool winusb)
        {
            DevicePath = devicePath;
            PnpDevice = device;
            IsWinUsb = winusb;

            InitializeComponent();

            if (IsWinUsb)
            {
                var usbDevice = USBDevice.GetSingleDeviceByPath(devicePath);
                manufacturerLabel.Content = usbDevice.Descriptor.Manufacturer;
                nameLabel.Content = usbDevice.Descriptor.Product;
                switchDriverButton.Content = "Revert Driver";
            }
            else
            {
                manufacturerLabel.Content = PnpDevice.GetProperty<string>(DevicePropertyKey.Device_Manufacturer);
                nameLabel.Content = PnpDevice.GetProperty<string>(DevicePropertyKey.NAME);
                switchDriverButton.Content = "Switch Driver";
            }
        }

        private void switchDriverButton_Clicked(object sender, RoutedEventArgs args)
        {
            if (IsWinUsb)
                RevertDriver();
            else
                SwitchToWinUSB();
        }

        private void SwitchToWinUSB()
        {
            // Attempt normally in case we already have admin permissions
            if (WinUsbBackend.SwitchDeviceToWinUSB(PnpDevice))
                return;

            // Otherwise, do it in a separate admin process
            if (!Program.StartWinUsbProcess(PnpDevice.InstanceId))
                MessageBox.Show("Failed to switch device to WinUSB!", "Failed To Switch Driver", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void RevertDriver()
        {
            // Attempt normally in case we already have admin permissions
            if (WinUsbBackend.RevertDevice(PnpDevice))
                return;

            // Otherwise, do it in a separate admin process
            if (!Program.StartRevertProcess(PnpDevice.InstanceId))
                MessageBox.Show("Failed to revert device to its original driver!", "Failed To Switch Driver", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
