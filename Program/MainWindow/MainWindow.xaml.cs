using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        /// Whether or not packet capture is active.
        /// </summary>
        private bool packetCaptureActive = false;

        /// <summary>
        /// Whether or not packets should be logged to a file.
        /// </summary>
        private bool packetDebugLog = false;

        /// <summary>
        /// Whether or not packets should be logged to a file.
        /// </summary>
        private bool firstPcapRefresh = true;

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
            ViGEmBus = 1
        }

        public MainWindow()
        {
            InitializeComponent();

            var version = Assembly.GetEntryAssembly().GetName().Version;
            versionLabel.Content = $"v{version}";
#if DEBUG
            versionLabel.Content += " Debug";
#endif

            // Capture Dispatcher object for use in callbacks
            uiDispatcher = Dispatcher;
        }

        /// <summary>
        /// Called when the window loads.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to console
            var textboxConsole = new TextBoxWriter(messageConsole);
            Console.SetOut(textboxConsole);

            // Check for Pcap
            try
            {
                var pcapDeviceList = CaptureDeviceList.Instance;
                pcapDeviceList.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not load Pcap interface! Pcap backend will be unavailable.");
                Logging.Main_WriteException(ex, "Failed to load Pcap interface!");

                // Force-disable Pcap backend
                Settings.Default.pcapEnabled = false;
                pcapEnabledCheckBox.IsEnabled = false;
            }

            // Load console/log settings
            SetPacketDebug(Settings.Default.packetDebug);
            SetPacketDebugLog(Settings.Default.packetDebugLog);
            SetVerboseErrors(Settings.Default.verboseErrorLog);

            // Check for vJoy
            bool vjoyFound = VjoyClient.Enabled;
            if (vjoyFound)
            {
                // Log vJoy driver attributes (Vendor Name, Product Name, Version Number)
                Console.WriteLine($"vJoy found! - Vendor: {VjoyClient.Manufacturer}, Product: {VjoyClient.Product}, Version Number: {VjoyClient.SerialNumber}");

                if (VjoyClient.GetAvailableDeviceCount() > 0)
                {
                    vjoyDeviceTypeOption.IsEnabled = true;
                }
                else
                {
                    Console.WriteLine("No vJoy devices found. vJoy selection will be unavailable.");
                    vjoyDeviceTypeOption.IsEnabled = false;
                    vjoyDeviceTypeOption.IsSelected = false;
                }
            }
            else
            {
                Console.WriteLine("No vJoy driver found, or vJoy is disabled. vJoy selection will be unavailable.");
                vjoyDeviceTypeOption.IsEnabled = false;
                vjoyDeviceTypeOption.IsSelected = false;
            }

            // Check for ViGEmBus
            bool vigemFound = VigemClient.TryInitialize();
            if (vigemFound)
            {
                Console.WriteLine("ViGEmBus found!");
                vigemDeviceTypeOption.IsEnabled = true;
            }
            else
            {
                Console.WriteLine("ViGEmBus not found. ViGEmBus selection will be unavailable.");
                vigemDeviceTypeOption.IsEnabled = false;
                vigemDeviceTypeOption.IsSelected = false;
            }

            // Load backend settings
            // Done after initializing virtual controller clients
            SetDeviceType((ControllerType)Settings.Default.controllerDeviceType);
            SetPcapEnabled(Settings.Default.pcapEnabled);
            SetUsbEnabled(Settings.Default.usbEnabled);

            // Exit if neither ViGEmBus nor vJoy are installed
            if (!vjoyFound && !vigemFound)
            {
                MessageBox.Show("No controller emulators found! Please install vJoy or ViGEmBus.\nThe program will now shut down.", "No Controller Emulators Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
        }

        /// <summary>
        /// Called when the window has closed.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Shut down
            if (packetCaptureActive)
            {
                StopCapture();
            }
            WinUsbBackend.Uninitialize();
            WinUsbBackend.DeviceAddedOrRemoved -= WinUsbDeviceAddedOrRemoved;

            // Clean up
            Settings.Default.Save();
            Logging.CloseAll();
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

            // Skip everything else if disabled
            if (!Settings.Default.pcapEnabled)
            {
                SetStartButtonState();
                return;
            }

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
                bool isSelected = device.Name == currentPcapSelection || device.Name == pcapSelectedDevice?.Name;

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

            if (firstPcapRefresh && pcapSelectedDevice == null)
            {
                pcapDeviceCombo.SelectedIndex = -1;
                Console.WriteLine("No Xbox controller receivers detected! Please ensure you have one connected, and that you are using WinPcap and not Npcap.");
                Console.WriteLine("You may need to run through auto-detection or manually select the device from the dropdown as well.");
            }

            int count = pcapDeviceList.Count;
            if (count == 1)
                Console.WriteLine("Discovered 1 Pcap device.");
            else
                Console.WriteLine($"Discovered {pcapDeviceList.Count} Pcap devices.");

            SetStartButtonState();

            firstPcapRefresh = false;
        }

        private void SetStartButtonState()
        {
            bool startEnabled = true;

            // At least one backend must be enabled
            bool backendEnabled = Settings.Default.usbEnabled || Settings.Default.pcapEnabled;
            usbEnabledCheckBox.FontWeight = !backendEnabled && usbEnabledCheckBox.IsEnabled ? FontWeights.Bold : FontWeights.Normal;
            pcapEnabledCheckBox.FontWeight = !backendEnabled && pcapEnabledCheckBox.IsEnabled ? FontWeights.Bold : FontWeights.Normal;
            startEnabled &= backendEnabled;

            // If Pcap is enabled, a capture device must be selected
            // If USB is also enabled, this condition is ignored
            bool pcapBold = Settings.Default.pcapEnabled && pcapDeviceCombo.SelectedIndex == -1;
            pcapBold &= !Settings.Default.usbEnabled;
            pcapDeviceLabel.FontWeight = pcapBold && pcapDeviceLabel.IsEnabled ? FontWeights.Bold : FontWeights.Normal;
            startEnabled &= !pcapBold;

            // Emulation type must be selected
            bool emulationTypeSelected = BackendSettings.MapperMode != MappingMode.NotSet;
            controllerDeviceTypeLabel.FontWeight = !emulationTypeSelected &&
                controllerDeviceTypeLabel.IsEnabled ? FontWeights.Bold : FontWeights.Normal;
            startEnabled &= emulationTypeSelected;

            // Enable start button if all the conditions above pass
            startButton.IsEnabled = startEnabled;
        }

        private void WinUsbDeviceAddedOrRemoved()
        {
            uiDispatcher.Invoke(() =>
            {
                SetStartButtonState();
                usbDeviceCountLabel.Content = $"Count: {WinUsbBackend.DeviceCount}";
            });
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
            pcapEnabledCheckBox.IsEnabled = false;
            pcapDeviceCombo.IsEnabled = false;
            pcapAutoDetectButton.IsEnabled = false;
            pcapRefreshButton.IsEnabled = false;
            usbEnabledCheckBox.IsEnabled = false;
            packetDebugCheckBox.IsEnabled = false;
            packetLogCheckBox.IsEnabled = false;
            verboseErrorCheckBox.IsEnabled = false;

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
            pcapEnabledCheckBox.IsEnabled = true;
            usbEnabledCheckBox.IsEnabled = true;
            SetPcapEnabled(Settings.Default.pcapEnabled);
            SetUsbEnabled(Settings.Default.usbEnabled);

            packetDebugCheckBox.IsEnabled = true;
            packetLogCheckBox.IsEnabled = true;
            verboseErrorCheckBox.IsEnabled = true;

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
            // Ignore if disabled or no device is selected
            if (!Settings.Default.pcapEnabled || pcapSelectedDevice == null)
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
            PcapBackend.OnCaptureStop += OnCaptureStopped;
            PcapBackend.StartCapture(pcapSelectedDevice);
            Console.WriteLine($"Listening on {pcapSelectedDevice.GetDisplayName()}...");

            return true;
        }

        private bool StartWinUsbCapture()
        {
            // Ignore if disabled
            if (!Settings.Default.usbEnabled)
                return true;

            WinUsbBackend.EnableInputs(true);
            return true;
        }

        private void StopPcapCapture()
        {
            // Ignore if disabled or no device is selected
            if (!Settings.Default.pcapEnabled || pcapSelectedDevice == null)
                return;

            PcapBackend.StopCapture();
        }

        private void StopWinUsbCapture()
        {
            // Ignore if disabled
            if (!Settings.Default.usbEnabled)
                return;

            WinUsbBackend.EnableInputs(false);
        }

        private void SetPcapEnabled(bool enabled)
        {
            if (pcapEnabledCheckBox.IsChecked != enabled)
            {
                // Let the event handler set everything
                pcapEnabledCheckBox.IsChecked = enabled;
                return;
            }

            Settings.Default.pcapEnabled = enabled;

            pcapDeviceLabel.IsEnabled = enabled;
            pcapDeviceCombo.IsEnabled = enabled;
            pcapAutoDetectButton.IsEnabled = enabled;
            pcapRefreshButton.IsEnabled = enabled;

            PopulatePcapDropdown();
        }

        private void SetUsbEnabled(bool enabled)
        {
            if (usbEnabledCheckBox.IsChecked != enabled)
            {
                usbEnabledCheckBox.IsChecked = enabled;
                return;
            }

            Settings.Default.usbEnabled = enabled;

            usbDeviceCountLabel.IsEnabled = enabled;
            usbConfigureDevicesButton.IsEnabled = enabled;

            if (WinUsbBackend.Initialized != enabled)
            {
                if (enabled)
                {
                    WinUsbBackend.DeviceAddedOrRemoved += WinUsbDeviceAddedOrRemoved;
                    WinUsbBackend.Initialize();
                }
                else
                {
                    WinUsbBackend.Uninitialize();
                    WinUsbBackend.DeviceAddedOrRemoved -= WinUsbDeviceAddedOrRemoved;
                }
            }

            usbDeviceCountLabel.Content = $"Count: {WinUsbBackend.DeviceCount}";
            SetStartButtonState();
        }

        private void SetPacketDebug(bool enabled)
        {
            if (packetDebugCheckBox.IsChecked != enabled)
            {
                packetDebugCheckBox.IsChecked = enabled;
                return;
            }

            Settings.Default.packetDebug = enabled;

            BackendSettings.LogPackets = enabled;
            packetLogCheckBox.IsEnabled = enabled;
            packetDebugLog = enabled && packetLogCheckBox.IsChecked.GetValueOrDefault();
        }

        private void SetPacketDebugLog(bool enabled)
        {
            if (packetLogCheckBox.IsChecked != enabled)
            {
                packetLogCheckBox.IsChecked = enabled;
                return;
            }

            packetDebugLog = Settings.Default.packetDebugLog = enabled;
        }

        private void SetVerboseErrors(bool enabled)
        {
            if (packetDebugCheckBox.IsChecked != enabled)
            {
                verboseErrorCheckBox.IsChecked = enabled;
                return;
            }

            Settings.Default.verboseErrorLog = enabled;
            BackendSettings.PrintVerboseErrors = enabled;
        }

        private void SetDeviceType(ControllerType type)
        {
            int typeInt = (int)type;
            if (controllerDeviceTypeCombo.SelectedIndex != typeInt)
            {
                // Set device type selection to the correct thing
                // Setting this fires off the handler, so we need to return
                // and let the second call set things
                controllerDeviceTypeCombo.SelectedIndex = typeInt;
                return;
            }

            Settings.Default.controllerDeviceType = typeInt;

            switch (type)
            {
                case ControllerType.vJoy:
                    if (vjoyDeviceTypeOption.IsEnabled && VjoyClient.GetAvailableDeviceCount() > 0)
                    {
                        BackendSettings.MapperMode = MappingMode.vJoy;
                    }
                    else
                    {
                        // Reset device type selection
                        // Setting this fires off the handler again, no extra handling is needed
                        BackendSettings.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                case ControllerType.ViGEmBus:
                    if (vigemDeviceTypeOption.IsEnabled && VigemClient.Initialized)
                    {
                        BackendSettings.MapperMode = MappingMode.ViGEmBus;
                    }
                    else
                    {
                        // Reset device type selection
                        // Setting this fires off the handler again, no extra handling is needed
                        BackendSettings.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                case ControllerType.None:
                default:
                    BackendSettings.MapperMode = MappingMode.NotSet;
                    break;
            }

            SetStartButtonState();
        }

        /// <summary>
        /// Handles Pcap device selection changes.
        /// </summary>
        private void pcapDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected combo box item
            if (!(pcapDeviceCombo.SelectedItem is ComboBoxItem selection))
            {
                // Disable start button
                SetStartButtonState();

                // Clear saved device
                Settings.Default.pcapDevice = String.Empty;
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
        /// Handles the verbose error checkbox being checked.
        /// </summary>
        private void pcapEnabledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool pcapEnabled = pcapEnabledCheckBox.IsChecked.GetValueOrDefault();
            SetPcapEnabled(pcapEnabled);
        }

        /// <summary>
        /// Handles the verbose error checkbox being checked.
        /// </summary>
        private void usbEnabledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool usbEnabled = usbEnabledCheckBox.IsChecked.GetValueOrDefault();
            SetUsbEnabled(usbEnabled);
        }

        /// <summary>
        /// Handles the packet debug checkbox being checked/unchecked.
        /// </summary>
        private void packetDebugCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool packetDebug = packetDebugCheckBox.IsChecked.GetValueOrDefault();
            SetPacketDebug(packetDebug);
        }

        /// <summary>
        /// Handles the packet debug checkbox being checked.
        /// </summary>
        private void packetLogCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool packetDebugLog = packetLogCheckBox.IsChecked.GetValueOrDefault();
            SetPacketDebugLog(packetDebugLog);
        }

        /// <summary>
        /// Handles the verbose error checkbox being checked.
        /// </summary>
        private void verboseErrorCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool verboseErrors = verboseErrorCheckBox.IsChecked.GetValueOrDefault();
            SetVerboseErrors(verboseErrors);
        }

        /// <summary>
        /// Handles the click of the Pcap Refresh button.
        /// </summary>
        private void pcapRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Re-populate dropdown
            PopulatePcapDropdown();
        }

        /// <summary>
        /// Handles the controller type setting being changed.
        /// </summary>
        private void controllerDeviceTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetDeviceType((ControllerType)controllerDeviceTypeCombo.SelectedIndex);
        }

        /// <summary>
        /// Handles the click of the USB Configure Devices button.
        /// </summary>
        private void usbConfigureDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new UsbDeviceListWindow();
            window.ShowDialog();
        }

        /// <summary>
        /// Handles the Pcap auto-detect button being clicked.
        /// </summary>
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
