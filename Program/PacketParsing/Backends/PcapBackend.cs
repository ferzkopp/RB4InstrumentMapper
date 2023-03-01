using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using SharpPcap;

namespace RB4InstrumentMapper.Parsing
{
    public delegate void PacketReceivedHandler(DateTime timestamp, ReadOnlySpan<byte> data);

    public static class PcapBackend
    {
        public static bool LogPackets = false;

        private static ILiveDevice captureDevice = null;
        private static readonly Dictionary<ulong, XboxDevice> devices = new Dictionary<ulong, XboxDevice>();
        private static bool canHandleNewDevices = true;

        public static event Action OnCaptureStop;

        /// <summary>
        /// Starts capturing packets from the given device.
        /// </summary>
        public static void StartCapture(ILiveDevice device)
        {
            // Open the device
            device.Open(new DeviceConfiguration()
            {
                Snaplen = 45, // Capture small packets
                Mode = DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, // Promiscuous mode with maximum speed
                ReadTimeout = 50 // Read timeout
            });

            // Configure packet receive event handler
            device.OnPacketArrival += OnPacketArrival;
            
            // Start capture
            device.StartCapture();
            captureDevice = device;
        }

        /// <summary>
        /// Stops capturing packets.
        /// </summary>
        public static void StopCapture()
        {
            if (captureDevice != null)
            {
                captureDevice.OnPacketArrival -= OnPacketArrival;
                captureDevice.Close();
                captureDevice = null;
            }

            // Clean up devices
            foreach (XboxDevice device in devices.Values)
            {
                device.Close();
            }
            devices.Clear();
        }

        /// <summary>
        /// Handles captured packets.
        /// </summary>
        private static unsafe void OnPacketArrival(object sender, PacketCapture packet)
        {
            // Read out receiver header
            var data = packet.Data;
            if (data.Length < sizeof(ReceiverHeader) || !MemoryMarshal.TryRead(data, out ReceiverHeader header))
            {
                return;
            }
            data = data.Slice(sizeof(ReceiverHeader));

            // Check if device ID has been encountered yet
            ulong deviceId = header.DeviceId;
            if (!devices.TryGetValue(deviceId, out var device))
            {
                if (!canHandleNewDevices)
                {
                    return;
                }

                try
                {
                    device = new XboxDevice(ParseMode);
                }
                catch (ParseException ex)
                {
                    canHandleNewDevices = false;
                    Console.WriteLine("Device limit reached, or an error occured when creating virtual device. No more devices will be registered.");
                    Console.WriteLine($"Exception: {ex.GetFirstLine()}");
                    return;
                }

                devices.Add(deviceId, device);
                Console.WriteLine($"Encountered new device with ID {deviceId.ToString("X12")}");
            }

            try
            {
                device.ParseCommand(data);
            }
            catch (ThreadAbortException)
            {
                // Don't log thread aborts, just return
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while handling packet: {ex.GetFirstLine()}");
                Logging.Main_WriteException(ex, "Context: Unhandled error during packet handling");

                // Stop capture
                OnCaptureStop.Invoke();
                return;
            }

            // Debugging (if enabled)
            if (LogPackets)
            {
                RawCapture raw = packet.GetPacket();
                string packetLogString = raw.Timeval.Date.ToString("yyyy-MM-dd hh:mm:ss.fff") + $" [{raw.PacketLength}] " + ParsingHelpers.ByteArrayToHexString(raw.Data);;
                Console.WriteLine(packetLogString);
                Logging.Packet_WriteLine(packetLogString);
            }
        }
    }
}