using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RB4InstrumentMapper.Parsing;
using RB4InstrumentMapper.Properties;
using RB4InstrumentMapper.Vigem;
using RB4InstrumentMapper.Vjoy;
using SharpPcap;

namespace RB4InstrumentMapper
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Dispatcher to send changes to UI.
        /// </summary>
        private static Dispatcher uiDispatcher = null;

        /// <summary>
        /// The selected Pcap device.
        /// </summary>
        private ILiveDevice pcapSelectedDevice = null;

        /// <summary>
        /// Flag indicating that packet capture is active.
        /// </summary>
        private bool packetCaptureActive = false;

        /// <summary>
        /// Flag indicating if packets should be shown.
        /// </summary>
        private bool packetDebug = false;

        /// <summary>
        /// Flag indicating if packets should be logged to a file.
        /// </summary>
        private bool packetDebugLog = false;

        /// <summary>
        /// Prefix for Pcap combo box items.
        /// </summary>
        private const string pcapComboBoxPrefix = "pcapDeviceComboBoxItem";

        /// <summary>
        /// Available controller emulation types.
        /// </summary>
        private enum ControllerType
        {
            None = -1,
            vJoy = 0,
            VigemBus = 1
        }

        public MainWindow()
        {
            InitializeComponent();

            var version = Assembly.GetEntryAssembly().GetName().Version;
            versionLabel.Content = $"v{version}";
#if DEBUG
            versionLabel.Content += " Debug";
#endif

            // Capture Dispatcher object for use in callback
            uiDispatcher = Dispatcher;
        }

        /// <summary>
        /// Called when the window loads.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to console
            TextBoxWriter.RedirectConsoleToTextBox(messageConsole, displayLinesWithTimestamp: false);

            // Load saved settings
            packetDebugCheckBox.IsChecked = Settings.Default.packetDebug;
            packetLogCheckBox.IsChecked = Settings.Default.packetDebugLog;
            int deviceType = controllerDeviceTypeCombo.SelectedIndex = Settings.Default.controllerDeviceType;

            // Check for vJoy
            bool vjoyFound = VjoyClient.Enabled;
            if (vjoyFound)
            {
                // Log vJoy driver attributes (Vendor Name, Product Name, Version Number)
                Console.WriteLine($"vJoy found! - Vendor: {VjoyClient.Manufacturer}, Product: {VjoyClient.Product}, Version Number: {VjoyClient.SerialNumber}");

                if (VjoyClient.GetAvailableDeviceCount() > 0)
                {
                    (controllerDeviceTypeCombo.Items[0] as ComboBoxItem).IsEnabled = true;
                }
                else
                {
                    // Reset device type selection if it was set to vJoy
                    if (deviceType == (int)ControllerType.vJoy)
                    {
                        controllerDeviceTypeCombo.SelectedIndex = (int)ControllerType.None;
                    }
                }
            }
            else
            {
                Console.WriteLine("No vJoy driver found, or vJoy is disabled. vJoy selection will be unavailable.");

                // Reset device type selection if it was set to vJoy
                if (deviceType == (int)ControllerType.vJoy)
                {
                    controllerDeviceTypeCombo.SelectedIndex = (int)ControllerType.None;
                }
            }

            // Check for ViGEmBus
            bool vigemFound = VigemClient.TryInitialize();
            if (vigemFound)
            {
                Console.WriteLine("ViGEmBus found!");
                (controllerDeviceTypeCombo.Items[1] as ComboBoxItem).IsEnabled = true;
            }
            else
            {
                vigemFound = false;
                Console.WriteLine("ViGEmBus not found. ViGEmBus selection will be unavailable.");

                // Reset device type selection if it was set to ViGEmBus
                if (deviceType == (int)ControllerType.VigemBus)
                {
                    controllerDeviceTypeCombo.SelectedIndex = (int)ControllerType.None;
                }
            }

            // Exit if neither ViGEmBus nor vJoy are installed
            if (!(vjoyFound || vigemFound))
            {
                MessageBox.Show("No controller emulators found! Please install vJoy or ViGEmBus.\nThe program will now shut down.", "No Controller Emulators Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // Initialize Pcap dropdown
            try
            {
                PopulatePcapDropdown();
            }
            catch (DllNotFoundException ex)
            {
                MessageBox.Show("Could not load Pcap interface! Please ensure WinPcap is installed on your system.", "Couldn't Find WinPcap", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();

                // Log exception
                Logging.Main_WriteException(ex, "Failed to load Pcap interface!");

                return;
            }

            WinUsbBackend.DeviceAddedOrRemoved += WinUsbDeviceAddedOrRemoved;
            WinUsbBackend.Initialize();
        }

        /// <summary>
        /// Called when the window has closed.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Shutdown
            if (packetCaptureActive)
            {
                StopCapture();
            }

            WinUsbBackend.Uninitialize();
            WinUsbBackend.DeviceAddedOrRemoved -= WinUsbDeviceAddedOrRemoved;

            // Close the log files
            Logging.CloseAll();

            // Dispose ViGEmBus
            VigemClient.Dispose();
        }

        /// <summary>
        /// Populates the Pcap device combo.
        /// </summary>
        /// <remarks>
        /// Used both when initializing, and when refreshing.
        /// </remarks>
        private void PopulatePcapDropdown()
        {
            // Clear combo list
            pcapDeviceCombo.Items.Clear();

            // Refresh the device list
            var pcapDeviceList = CaptureDeviceList.Instance;
            pcapDeviceList.Refresh();
            if (pcapDeviceList.Count == 0)
            {
                Console.WriteLine("No Pcap devices found!");
                return;
            }

            // Load saved device name
            string currentPcapSelection = Settings.Default.pcapDevice;

            // Populate combo and print the list
            for (int i = 0; i < pcapDeviceList.Count; i++)
            {
                var device = pcapDeviceList[i];
                string itemNumber = $"{i + 1}";

                string deviceName = $"{itemNumber}. {device.GetDisplayName()}";
                string itemName = pcapComboBoxPrefix + itemNumber;
                bool isSelected = device.Name.Equals(currentPcapSelection) || device.Name.Equals(pcapSelectedDevice?.Name);

                if (isSelected || (string.IsNullOrEmpty(currentPcapSelection) && device.IsXboxOneReceiver()))
                {
                    pcapSelectedDevice = device;
                    isSelected = true;
                }

                pcapDeviceCombo.Items.Add(new ComboBoxItem()
                {
                    Name = itemName,
                    Content = deviceName,
                    IsEnabled = true,
                    IsSelected = isSelected
                });
            }

            if (pcapSelectedDevice == null)
            {
                pcapDeviceCombo.SelectedIndex = -1;
                Console.WriteLine("No Xbox controller receivers detected during refresh! Please ensure you have one connected, and that you are using WinPcap and not Npcap.");
                Console.WriteLine("You may need to run through auto-detection or manually select the device from the dropdown as well.");
            }

            Console.WriteLine($"Discovered {pcapDeviceList.Count} Pcap devices.");
        }

        private void SetStartButtonState()
        {
            startButton.IsEnabled =
                WinUsbBackend.DeviceCount > 0 ||
                packetCaptureActive ||
                (controllerDeviceTypeCombo.SelectedIndex != (int)ControllerType.None &&
                pcapDeviceCombo.SelectedIndex != -1);
        }

        private void WinUsbDeviceAddedOrRemoved()
        {
            uiDispatcher.Invoke(SetStartButtonState);
        }

        /// <summary>
        /// Configures the Pcap device and controller devices, and starts packet capture.
        /// </summary>
        private void StartCapture()
        {
            if (!StartPcapCapture() || !StartWinUsbCapture())
            {
                StopPcapCapture();
                StopWinUsbCapture();
                return;
            }

            // Enable packet capture active flag
            packetCaptureActive = true;

            // Set window controls
            pcapDeviceCombo.IsEnabled = false;
            pcapAutoDetectButton.IsEnabled = false;
            pcapRefreshButton.IsEnabled = false;
            packetDebugCheckBox.IsEnabled = false;
            packetLogCheckBox.IsEnabled = false;

            controllerDeviceTypeCombo.IsEnabled = false;

            startButton.Content = "Stop";

            // Initialize packet log
            if (packetDebugLog)
            {
                if (!Logging.CreatePacketLog())
                {
                    packetDebugLog = false;
                    // Remaining context for this message is inside of the log creation
                    Console.WriteLine("Disabled packet logging for this capture session.");
                }
            }
        }

        private void OnCaptureStopped()
        {
            PcapBackend.OnCaptureStop -= OnCaptureStopped;
            uiDispatcher.Invoke(StopCapture);
        }

        /// <summary>
        /// Stops packet capture/mapping and resets Pcap/controller objects.
        /// </summary>
        private void StopCapture()
        {
            StopPcapCapture();
            StopWinUsbCapture();

            // Store whether or not the packet log was created
            bool doPacketLogMessage = Logging.PacketLogExists;
            // Close packet log file
            Logging.ClosePacketLog();

            // Disable packet capture active flag
            packetCaptureActive = false;

            // Set window controls
            pcapDeviceCombo.IsEnabled = true;
            pcapAutoDetectButton.IsEnabled = true;
            pcapRefreshButton.IsEnabled = true;
            packetDebugCheckBox.IsEnabled = true;
            packetLogCheckBox.IsEnabled = true;

            controllerDeviceTypeCombo.IsEnabled = true;

            startButton.Content = "Start";

            // Force a refresh of the controller textbox
            controllerDeviceTypeCombo_SelectionChanged(null, null);

            Console.WriteLine("Stopped capture.");
            if (doPacketLogMessage)
            {
                Console.WriteLine($"Packet logs may be found in {Logging.PacketLogFolderPath}");
            }
        }

        private bool StartPcapCapture()
        {
            // Ignore if no device is selected
            if (pcapSelectedDevice == null)
                return true;

            // Check if the device is still present
            var pcapDeviceList = CaptureDeviceList.Instance;
            pcapDeviceList.Refresh();
            bool deviceStillPresent = false;
            foreach (var device in pcapDeviceList)
            {
                if (device.Name == pcapSelectedDevice.Name)
                {
                    deviceStillPresent = true;
                    break;
                }
            }

            if (!deviceStillPresent)
            {
                // Invalidate selected device (but not the saved preference)
                pcapSelectedDevice = null;

                // Notify user
                MessageBox.Show(
                    "Pcap device list has changed and the selected device is no longer present.\nPlease re-select your device from the list and try again.",
                    "Pcap Device Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation
                );

                // Force a refresh
                PopulatePcapDropdown();
                return false;
            }

            // Start capture
            BackendSettings.LogPackets = packetDebug;
            PcapBackend.OnCaptureStop += OnCaptureStopped;
            PcapBackend.StartCapture(pcapSelectedDevice);
            Console.WriteLine($"Listening on {pcapSelectedDevice.GetDisplayName()}...");

            return true;
        }

        private bool StartWinUsbCapture()
        {
            WinUsbBackend.Start();
            return true;
        }

        private void StopPcapCapture()
        {
            PcapBackend.StopCapture();
        }

        private void StopWinUsbCapture()
        {
            WinUsbBackend.Stop();
        }

        /// <summary>
        /// Handles Pcap device selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected combo box item
            if (!(pcapDeviceCombo.SelectedItem is ComboBoxItem selection))
            {
                // Disable start button
                SetStartButtonState();

                // Clear saved device
                Settings.Default.pcapDevice = String.Empty;
                Settings.Default.Save();
                return;
            }
            string itemName = selection.Name;

            // Get index of selected Pcap device
            if (int.TryParse(itemName.Substring(pcapComboBoxPrefix.Length), out int pcapDeviceIndex))
            {
                // Adjust index count (UI->Logical)
                pcapDeviceIndex -= 1;

                // Assign device
                pcapSelectedDevice = CaptureDeviceList.Instance[pcapDeviceIndex];
                Console.WriteLine($"Selected Pcap device {pcapSelectedDevice.GetDisplayName()}");

                // Enable start button
                SetStartButtonState();

                // Remember selected Pcap device's name
                Settings.Default.pcapDevice = pcapSelectedDevice.Name;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handles the click of the Start button.
        /// </summary>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (!packetCaptureActive)
            {
                StartCapture();
            }
            else
            {
                StopCapture();
            }
        }

        /// <summary>
        /// Handles the packet debug checkbox being checked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetDebugCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            packetDebug = true;
            packetLogCheckBox.IsEnabled = true;
            packetDebugLog = packetLogCheckBox.IsChecked.GetValueOrDefault();

            // Remember selected packet debug state
            Settings.Default.packetDebug = true;
            Settings.Default.Save();
        }

        /// <summary>
        /// Handles the packet debug checkbox being unchecked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetDebugCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            packetDebug = false;
            packetLogCheckBox.IsEnabled = false;
            packetDebugLog = false;

            // Remember selected packet debug state
            Settings.Default.packetDebug = false;
            Settings.Default.Save();
        }

        /// <summary>
        /// Handles the packet debug checkbox being checked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetLogCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            packetDebugLog = true;

            // Remember selected packet debug state
            Settings.Default.packetDebugLog = true;
            Settings.Default.Save();
        }

        /// <summary>
        /// Handles the packet debug checkbox being unchecked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetLogCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            packetDebugLog = false;

            // Remember selected packet debug state
            Settings.Default.packetDebugLog = false;
            Settings.Default.Save();
        }

        /// <summary>
        /// Handles the click of the Pcap Refresh button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Re-populate dropdown
            PopulatePcapDropdown();
        }

        private void controllerDeviceTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Set parsing mode
            switch (controllerDeviceTypeCombo.SelectedIndex)
            {
                // vJoy
                case 0:
                    if (VjoyClient.GetAvailableDeviceCount() > 0)
                    {
                        BackendSettings.MapperMode = MappingMode.vJoy;
                        Settings.Default.controllerDeviceType = (int)ControllerType.vJoy;
                    }
                    else
                    {
                        // Reset device type selection
                        // The parse mode and saved setting will get set automatically since setting this fires off this handler again
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                // ViGEmBus
                case 1:
                    BackendSettings.MapperMode = MappingMode.ViGEmBus;
                    Settings.Default.controllerDeviceType = (int)ControllerType.VigemBus;
                    break;

                default:
                    BackendSettings.MapperMode = 0;
                    Settings.Default.controllerDeviceType = (int)ControllerType.None;
                    break;
            }

            // Save setting
            Settings.Default.controllerDeviceType = controllerDeviceTypeCombo.SelectedIndex;
            Settings.Default.Save();

            // Enable start button
            SetStartButtonState();
        }

        /// <summary>
        /// Handles the Pcap auto-detect button being clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapAutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result;

            // Refresh and check for Xbox One receivers
            var pcapDeviceList = CaptureDeviceList.Instance;
            pcapDeviceList.Refresh();
            bool foundDevice = false;
            foreach (var device in pcapDeviceList)
            {
                if (!device.IsXboxOneReceiver())
                {
                    continue;
                }

                foundDevice = true;
                result = MessageBox.Show(
                    $"Found Xbox One receiver device: {device.GetDisplayName()}\nPress OK to set this device as your selected Pcap device, or press Cancel to continue with the auto-detection process.",
                    "Auto-Detect Receiver",
                    MessageBoxButton.OKCancel
                );
                if (result == MessageBoxResult.OK)
                {
                    // Assign the new device
                    pcapSelectedDevice = device;

                    // Remember the new device
                    Settings.Default.pcapDevice = pcapSelectedDevice.Name;
                    Settings.Default.Save();

                    // Refresh the dropdown
                    PopulatePcapDropdown();
                    return;
                }
                else
                {
                    continue;
                }
            }

            if (!foundDevice)
            {
                result = MessageBox.Show(
                    "No Xbox One receivers could be found through checking device \nYou will now be guided through a second auto-detection process. Press Cancel at any time to cancel the process.",
                    "Auto-Detect Receiver",
                    MessageBoxButton.OKCancel
                );
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // Prompt user to unplug their receiver
            result = MessageBox.Show(
                "Unplug your receiver, then click OK.",
                "Auto-Detect Receiver",
                MessageBoxButton.OKCancel
            );
            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            // Get the list of devices for when receiver is unplugged
            var notPlugged = CaptureDeviceList.New();

            // Prompt user to plug in their receiver
            result = MessageBox.Show(
                "Now plug in your receiver, wait a bit for it to register, then click OK.",
                "Auto-Detect Receiver",
                MessageBoxButton.OKCancel
            );
            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            // Get the list of devices for when receiver is plugged in
            var plugged = CaptureDeviceList.New();

            // Get device names for both not plugged and plugged lists
            var notPluggedNames = new List<string>();
            var pluggedNames = new List<string>();
            foreach (var oldDevice in notPlugged)
            {
                notPluggedNames.Add(oldDevice.Name);
            }
            foreach (var newDevice in plugged)
            {
                pluggedNames.Add(newDevice.Name);
            }

            // Compare the lists and find what notPlugged doesn't contain
            var newNames = new List<string>();
            foreach (string pluggedName in pluggedNames)
            {
                if (!notPluggedNames.Contains(pluggedName))
                {
                    newNames.Add(pluggedName);
                }
            }

            // Create a list of new devices based on the list of new device names
            var newDevices = new List<ILiveDevice>();
            foreach (var newDevice in plugged)
            {
                if (newNames.Contains(newDevice.Name))
                {
                    newDevices.Add(newDevice);
                }
            }

            // If there's (strictly) one new device, assign it
            if (newDevices.Count == 1)
            {
                // Assign the new device
                pcapSelectedDevice = newDevices.First();

                // Remember the new device
                Settings.Default.pcapDevice = pcapSelectedDevice.Name;
                Settings.Default.Save();
            }
            else
            {
                // If there's more than one, don't auto-assign any of them
                if (newDevices.Count > 1)
                {
                    MessageBox.Show("Could not auto-assign; more than one new device was detected.", "Auto-Detect Receiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                // If there's no new ones, don't do anything
                else if (newDevices.Count == 0)
                {
                    MessageBox.Show("Could not auto-assign; no new devices were detected.", "Auto-Detect Receiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Refresh the dropdown
            PopulatePcapDropdown();
        }
    }
}
