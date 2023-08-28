using System.Windows;
using System.Windows.Controls;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

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
        }
    }
}
