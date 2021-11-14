using PcapDotNet.Core;
using PcapDotNet.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using vJoyInterfaceWrap;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Dispatcher to send changes to UI.
        /// </summary>
        private static Dispatcher uiDispatcher = null;

        /// <summary>
        /// Default pcap packet capture timeout in milliseconds.
        /// </summary>
        private const int DefaultPacketCaptureTimeoutMilliseconds = 50;

        /// <summary>
        /// Selected pCap device.
        /// </summary>
        private int pcapDeviceIndex = -1;

        /// <summary>
        /// Communicator object that continously reads the WinPcap of selected device.
        /// </summary>
        private PacketCommunicator pcapCommunicator;

        /// <summary>
        /// Thread that handles the capture
        /// </summary>
        private Thread pcapCaptureThread;

        /// <summary>
        /// Flasg indicating if packets should be shown.
        /// </summary>
        private static bool packetDebug = false;

        /// <summary>
        /// Common name for pcap combo box items.
        /// </summary>
        private const string pcapComboBoxItemName = "pcapDeviceComboBoxItem";

        /// <summary>
        /// Common name for vjoy combo box items.
        /// </summary>
        private const string vjoyComboBoxItemName = "vjoyComboBoxItem";

        /// <summary>
        /// Common joystick object and a position structure.
        /// </summary>
        private static vJoy joystick;

        /// <summary>
        /// Selected guitar 1 device
        /// </summary>
        private static uint guitar1DeviceIndex = 0;

        /// <summary>
        /// Packet instrument ID for guitar 1 device
        /// </summary>
        private static uint guitar1InstrumentId = 0; // This assumes that an ID of 0x00000000 is invalid

        /// <summary>
        /// Analyzed packet for guitar 1 device
        /// </summary>
        private static GuitarPacket guitar1Packet = new GuitarPacket();

        /// <summary>
        /// Selected guitar 2 device
        /// </summary>
        private static uint guitar2DeviceIndex = 0;

        /// <summary>
        /// Packet instrument ID for guitar 2 device
        /// </summary>
        private static uint guitar2InstrumentId = 0; // This assumes that an ID of 0x00000000 is invalid

        /// <summary>
        /// Analyzed packet for guitar 2 device
        /// </summary>
        private static GuitarPacket guitar2Packet = new GuitarPacket();

        /// <summary>
        /// Selected drum device.
        /// </summary>
        private static uint drumDeviceIndex = 0;

        /// <summary>
        /// Packet instrument ID for drum device
        /// </summary>
        private static uint drumInstrumentId = 0; // This assumes that an ID of 0x00000000 is invalid

        /// <summary>
        /// Analyzed packet for drum device
        /// </summary>
        private static DrumPacket drumPacket = new DrumPacket();

        /// <summary>
        /// Main window handler
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Capture Dispatcher object for use in callback
            uiDispatcher = this.Dispatcher;
        }

        /// <summary>
        /// Startup program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to console
            TextBoxConsole.RedirectConsoleToTextBox(messageConsole);

            // Initialize dropdowns
            PopulatePcapDropdown();
            PopulateVjoyDropdowns();
        }

        /// <summary>
        /// Shutdown program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Shutdown
            StopCapture();
        }

        /// <summary>
        /// Acquire vJoy device
        /// </summary>
        /// <param name="joystick">The vJoy object</param>
        /// <param name="deviceId">The device Id to acquire</param>
        static void AcquireJoystick(vJoy joystick, uint deviceId)
        {
            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(deviceId);

            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(deviceId))))
            {
                Console.WriteLine($"Failed to acquire vJoy device number {deviceId}.");
                return;
            }
            else
            {
                // Get the number of buttons 
                int nButtons = joystick.GetVJDButtonNumber(deviceId);

                Console.WriteLine($"Acquired: vJoy device number {deviceId} with {nButtons} buttons.");
            }
        }

        /// <summary>
        /// Populate vJoy combos.
        /// </summary>
        private void PopulateVjoyDropdowns()
        {
            // Create one joystick object and a position structure.
            joystick = new vJoy();

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled())
            {
                Console.WriteLine("No vJoy driver found. Make sure vJoy is installed and configured.");
                return;
            }
            
            Console.WriteLine($"vJoy found - Vendor: " + joystick.GetvJoyManufacturerString() + ", Product: " + joystick.GetvJoyProductString() + ", Version Number: " + joystick.GetvJoySerialNumberString());

            // Get default settings
            string currentGuitar1Selection = Properties.Settings.Default.currentGuitar1Selection;
            string currentGuitar2Selection = Properties.Settings.Default.currentGuitar2Selection;
            string currentDrumSelection = Properties.Settings.Default.currentDrumSelection;

            // Loop through IDs and populate dropdowns
            int freeDeviceCount = 0;
            for (uint id = 1; id <= 16; id++)
            {
                string deviceName = $"vJoy Device {id}";
                string itemName = $"{controllerComboBoxItemName}{id}";
                bool isEnabled = false;

                // Get the state of the requested device
                VjdStat status = joystick.GetVJDStatus(id);
                switch (status)
                {
                    case VjdStat.VJD_STAT_OWN:
                        deviceName += " (device is already owned by this feeder)";
                        break;
                    case VjdStat.VJD_STAT_FREE:
                        isEnabled = true;
                        freeDeviceCount++;
                        break;
                    case VjdStat.VJD_STAT_BUSY:
                        deviceName += " (device is already owned by this feeder)";
                        break;
                    case VjdStat.VJD_STAT_MISS:
                        deviceName += " (device is not installed or disabled)";
                        break;
                    default:
                        deviceName += " (general error)";
                        break;
                };

                // Guitar 1 combo item
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = deviceName;
                comboBoxItem.Name = itemName;
                comboBoxItem.IsEnabled = isEnabled;
                comboBoxItem.IsSelected = itemName.Equals(currentGuitar1Selection) && isEnabled;
                vjoyGuitar1Combo.Items.Add(comboBoxItem);

                // Guitar 2 combo item
                comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = deviceName;
                comboBoxItem.Name = itemName;
                comboBoxItem.IsEnabled = isEnabled;
                comboBoxItem.IsSelected = itemName.Equals(currentGuitar2Selection) && isEnabled;
                vjoyGuitar2Combo.Items.Add(comboBoxItem);

                // Drum combo item
                comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = deviceName;
                comboBoxItem.Name = itemName;
                comboBoxItem.IsEnabled = isEnabled;
                comboBoxItem.IsSelected = itemName.Equals(currentDrumSelection) && isEnabled;
                vjoyDrumCombo.Items.Add(comboBoxItem);
            }

            Console.WriteLine($"Discovered {freeDeviceCount} free vJoy devices.");

            // Preset device IDs
            
            // Guitar 1
            string hexString = Properties.Settings.Default.currentGuitar1Id;
            guitar1InstrumentId = Convert.ToUInt32(hexString, 16);
            guitar1IdTextBox.Text = (guitar1InstrumentId == 0) ? string.Empty : hexString;
            
            // Guitar 2
            hexString = Properties.Settings.Default.currentGuitar2Id;
            guitar2InstrumentId = Convert.ToUInt32(hexString, 16);
            guitar2IdTextBox.Text = (guitar2InstrumentId == 0) ? string.Empty : hexString;
            
            // Drum
            hexString = Properties.Settings.Default.currentDrumId;
            drumInstrumentId = Convert.ToUInt32(hexString, 16);
            drumIdTextBox.Text = (drumInstrumentId == 0) ? string.Empty : hexString;
        }

        /// <summary>
        /// Populate WinPcap device combos.
        /// </summary>
        private void PopulatePcapDropdown()
        {
            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (allDevices.Count == 0)
            {
                Console.WriteLine("No WinPcap interfaces found! Make sure WinPcap is installed.");
                return;
            }

            // Get default settings
            string currentPcapSelection = Properties.Settings.Default.currentPcapSelection;

            // Populate combo and print the list
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < allDevices.Count; i++)
            {
                LivePacketDevice device = allDevices[i];
                sb.Clear();
                string itemNumber = $"{i + 1}";
                sb.Append($"{itemNumber}. {device.Name}");
                if (device.Description != null)
                {
                    sb.Append($" ({device.Description})");
                }

                string deviceName = sb.ToString();
                string itemName = pcapComboBoxItemName + itemNumber;
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Name = itemName;
                comboBoxItem.Content = deviceName;
                comboBoxItem.IsEnabled = true;
                comboBoxItem.IsSelected = itemName.Equals(currentPcapSelection);
                pcapDeviceCombo.Items.Add(comboBoxItem);
            }

            // Preset debugging flag
            string currentPacketDebugState = Properties.Settings.Default.currentPacketDebugState;
            if (currentPacketDebugState == "true")
            {
                packetDebugCheckBox.IsChecked = true;
            }

            Console.WriteLine($"Discovered {allDevices.Count} WinPcap devices.");
        }

        /// <summary>
        /// Handle pcap device selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected pcap device
            ComboBoxItem typeItem = (ComboBoxItem)pcapDeviceCombo.SelectedItem;
            string itemName = typeItem.Name;

            // Get index of selected pcap device
            if (int.TryParse(itemName.Substring(pcapComboBoxItemName.Length), out pcapDeviceIndex))
            {
                // Adjust index count (UI->Logical)
                pcapDeviceIndex -= 1;

                // Remember selected pcap device
                Properties.Settings.Default.currentPcapSelection = itemName;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handle guitar selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vjoyGuitarCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected guitar device
            ComboBoxItem typeItem = (ComboBoxItem)vjoyGuitar1Combo.SelectedItem;
            string itemName = typeItem.Name;

            // Get index of selected guitar device
            if (uint.TryParse(typeItem.Name.Substring(vjoyComboBoxItemName.Length), out guitar1DeviceIndex))
            {
                // Remember selected guitar device
                Properties.Settings.Default.currentGuitar1Selection = itemName;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handle guitar 2 selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vjoyGuitar2Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected guitar device
            ComboBoxItem typeItem = (ComboBoxItem)vjoyGuitar2Combo.SelectedItem;
            string itemName = typeItem.Name;

            // Get index of selected guitar device
            if (uint.TryParse(typeItem.Name.Substring(vjoyComboBoxItemName.Length), out guitar2DeviceIndex))
            {
                // Remember selected guitar device
                Properties.Settings.Default.currentGuitar2Selection = itemName;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handle drum selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vjoyDrumCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected drum device
            ComboBoxItem typeItem = (ComboBoxItem)vjoyDrumCombo.SelectedItem;
            string itemName = typeItem.Name;

            // Get index of selected drum device
            if (uint.TryParse(typeItem.Name.Substring(vjoyComboBoxItemName.Length), out drumDeviceIndex))
            {
                // Remember selected drum device
                Properties.Settings.Default.currentDrumSelection = itemName;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Configure pcap device and start packet capture.
        /// </summary>
        /// <param name="deviceIndex">pcap device index</param>
        private void StartCapture(int deviceIndex)
        {
            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

            // Take the selected adapter
            PacketDevice selectedDevice = allDevices[deviceIndex];

            // Open the device
            pcapCommunicator =
                selectedDevice.Open(
                    45, // small packets
                    PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.MaximumResponsiveness, // promiscuous mode with maximum speed
                    DefaultPacketCaptureTimeoutMilliseconds); // read timeout
            // Read data
            pcapCaptureThread = new Thread(ReadContinously);
            pcapCaptureThread.Start();

            Console.WriteLine($"Listening on {selectedDevice.Description}...");
        }

        /// <summary>
        /// Continously read messages from pcap device while it is active.
        /// </summary>
        private void ReadContinously()
        {
            // start the capture
            pcapCommunicator.ReceivePackets(0, PacketHandler);
        }

        /// <summary>
        /// Callback function invoked by Pcap.Net for every incoming packet
        /// </summary>
        /// <param name="packet">The received packet</param>
        private void PacketHandler(Packet packet)
        {
            // Debugging (if enabled)
            if (packetDebug)
            {
                string packetHexString = ParsingHelpers.ByteArrayToHexString(packet.Buffer);
                Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + $" [{packet.Length}] " + packetHexString);
            }

            // Map drum (if enabled)
            if (joystick != null && drumDeviceIndex > 0)
            {
                if (DrumPacketReader.AnalyzePacket(packet.Buffer, out drumPacket))
                {
                    if (DrumPacketVjoyMapper.MapPacket(drumPacket, joystick, drumDeviceIndex, drumInstrumentId))
                    {
                        // Auto-populate instrument ID for drum
                        if (drumInstrumentId == 0)
                        {
                            // Copy from packet
                            drumInstrumentId = drumPacket.InstrumentID;

                            // Update UI
                            uiDispatcher.Invoke((Action)(() =>
                            {
                                drumIdTextBox.Text = (drumInstrumentId == 0) ? string.Empty : Convert.ToString(drumInstrumentId, 16);
                            }));
                        }

                        // Used packet
                        return;
                    }
                }
            }

            // Map guitar 1 (if enabled)
            if (joystick != null && guitar1DeviceIndex > 0)
            {
                if (GuitarPacketReader.AnalyzePacket(packet.Buffer, out guitar1Packet))
                {
                    if (GuitarPacketVjoyMapper.MapPacket(guitar1Packet, joystick, guitar1DeviceIndex, guitar1InstrumentId))
                    {
                        // Auto-populate instrument ID for guitar 1 if it wasn't set
                        if (guitar1InstrumentId == 0)
                        {
                            // Must be different from guitar 2
                            if (guitar2Packet.InstrumentID == guitar1Packet.InstrumentID)
                            {
                                return;
                            }

                            // Copy from packet
                            guitar1InstrumentId = guitar1Packet.InstrumentID;

                            // Update UI
                            uiDispatcher.Invoke((Action)(() =>
                            {
                                guitar1IdTextBox.Text = (guitar1InstrumentId == 0) ? string.Empty : Convert.ToString(guitar1InstrumentId, 16);
                            }));
                        }

                        // Used packet
                        return;
                    }
                }
            }

            // Map guitar 2 (if enabled)
            if (joystick != null && guitar2DeviceIndex > 0)
            {
                if (GuitarPacketReader.AnalyzePacket(packet.Buffer, out guitar2Packet))
                {
                    if (GuitarPacketVjoyMapper.MapPacket(guitar2Packet, joystick, guitar1DeviceIndex, guitar1InstrumentId))
                    {
                        // Auto-populate instrument ID for guitar 2 if it wasn't set
                        if (guitar2InstrumentId == 0)
                        {
                            // Must be different from guitar 1
                            if (guitar1Packet.InstrumentID == guitar2Packet.InstrumentID)
                            {
                                return;
                            }

                            // Copy from packet
                            guitar2InstrumentId = guitar2Packet.InstrumentID;

                            // Update UI
                            uiDispatcher.Invoke((Action)(() =>
                            {
                                guitar2IdTextBox.Text = (guitar2InstrumentId == 0) ? string.Empty : Convert.ToString(guitar2InstrumentId, 16);
                            }));
                        }

                        // Used packet
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Stop capture and mapping of packets
        /// </summary>
        private void StopCapture()
        {
            // Stop packet capture
            if (pcapCommunicator != null)
            {
                pcapCommunicator.Break();
                pcapCommunicator = null;
            }

            // Stop processing thread
            if (pcapCaptureThread != null)
            {
                pcapCaptureThread.Join();
                pcapCaptureThread = null;
            }

            // Release drum device
            if (joystick != null && drumDeviceIndex > 0)
            {
                joystick.RelinquishVJD(drumDeviceIndex);
            }

            // Release guitar device
            if (joystick != null && guitar1DeviceIndex > 0)
            {
                joystick.RelinquishVJD(guitar1DeviceIndex);
            }
        }

        /// <summary>
        /// Handle 'Start' button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            // Initialize vJoy
            if (joystick != null)
            {
                // Reset buttons and axis
                joystick.ResetAll();

                // Acquire vJoy devices
                if (guitar1DeviceIndex > 0)
                {
                    AcquireJoystick(joystick, guitar1DeviceIndex);
                }

                if (guitar2DeviceIndex > 0)
                {
                    AcquireJoystick(joystick, guitar2DeviceIndex);
                }

                if (drumDeviceIndex > 0)
                {
                    AcquireJoystick(joystick, drumDeviceIndex);
                }
            }

            // Start capture
            StartCapture(pcapDeviceIndex);

            // Only allow to start once
            startButton.IsEnabled = false;
        }

        /// <summary>
        /// Handle packet debug checkbox: enable packet output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetDebugCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            packetDebug = true;

            // Remember selected pcap debugging state
            Properties.Settings.Default.currentPacketDebugState = "true";
            Properties.Settings.Default.Save();

        }

        /// <summary>
        /// Handle packet debug checkbox: disable packet output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetDebugCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            packetDebug = false;

            // Remember selected pcap debugging state
            Properties.Settings.Default.currentPacketDebugState = "false";
            Properties.Settings.Default.Save();
        }

        private void guitar1IdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set new ID
            string hexString = guitar1IdTextBox.Text;
            if (string.IsNullOrEmpty(hexString))
            {
                Console.WriteLine("Cleared Hex ID for Guitar 1.");
                guitar1InstrumentId = 0;
                hexString = string.Empty;
            }
            else
            {
                guitar1InstrumentId = Convert.ToUInt32(hexString, 16);
                if (guitar1InstrumentId == 0)
                {
                    Console.WriteLine("Invalid Hex ID for Guitar 1.");
                    hexString = string.Empty;
                }
                else
                {
                    Console.WriteLine($"Set Guitar 1 instrument ID to {guitar1InstrumentId}.");
                }
            }

            // Remember guitar 1 ID
            Properties.Settings.Default.currentGuitar1Id = hexString;
            Properties.Settings.Default.Save();
        }

        private void guitar2IdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set new ID
            string hexString = guitar2IdTextBox.Text;
            if (string.IsNullOrEmpty(hexString))
            {
                Console.WriteLine("Cleared Hex ID for Guitar 2.");
                guitar2InstrumentId = 0;
                hexString = string.Empty;
            }
            else
            {
                guitar2InstrumentId = Convert.ToUInt32(hexString, 16);
                if (guitar2InstrumentId == 0)
                {
                    Console.WriteLine("Invalid Hex ID for Guitar 2.");
                    hexString = string.Empty;
                }
                else
                {
                    Console.WriteLine($"Set Guitar 2 instrument ID to {guitar2InstrumentId}.");
                }
            }

            // Remember guitar 2 ID
            Properties.Settings.Default.currentGuitar2Id = hexString;
            Properties.Settings.Default.Save();
        }

        private void drumIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set new ID
            string hexString = drumIdTextBox.Text;
            if (string.IsNullOrEmpty(hexString))
            {
                Console.WriteLine("Cleared Hex ID for Drum.");
                hexString = string.Empty;
                drumInstrumentId = 0;
            }
            else
            {
                // Parse
                drumInstrumentId = Convert.ToUInt32(hexString, 16);
                if (drumInstrumentId == 0)
                {
                    Console.WriteLine("Invalid Hex ID for Drum.");
                    hexString = string.Empty;
                }
                else
                {
                    Console.WriteLine($"Set Drums instrument ID to {drumInstrumentId}.");
                }
            }

            // Remember drum ID
            Properties.Settings.Default.currentDrumId = hexString;
            Properties.Settings.Default.Save();
        }
    }
}
