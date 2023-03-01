using System;
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

            try
            {
                PacketParser.HandlePacket(header.DeviceId, data);
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