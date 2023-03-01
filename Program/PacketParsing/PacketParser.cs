using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Emulated devices that can be parsed to.
    /// </summary>
    public enum ParsingMode
    {
        ViGEmBus = 1,
        vJoy = 2
    }

    /// <summary>
    /// Handles packets from a capture device.
    /// </summary>
    public static class PacketParser
    {
        /// <summary>
        /// Device IDs detected during Pcap.
        /// </summary>
        private static Dictionary<ulong, XboxDevice> pcapIds = new Dictionary<ulong, XboxDevice>();

        /// <summary>
        /// Gets or sets the current parsing mode.
        /// </summary>
        public static ParsingMode ParseMode { get; set; } = (ParsingMode)0;

        /// <summary>
        /// Whether or not new devices can be added.
        /// </summary>
        private static bool canHandleNewDevices = true;

        /// <summary>
        /// Handles a received Pcap packet.
        /// </summary>
        public static unsafe void HandlePacket(ulong deviceId, ReadOnlySpan<byte> data)
        {
            // Check if device ID has been encountered yet
            if (!pcapIds.ContainsKey(deviceId))
            {
                if (!canHandleNewDevices)
                {
                    return;
                }

                XboxDevice device;
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

                pcapIds.Add(deviceId, device);
                Console.WriteLine($"Encountered new device with ID {deviceId.ToString("X12")}");
            }

            // Parse data
            pcapIds[deviceId].ParseCommand(data);
        }

        // TODO: Add libusb support

        /// <summary>
        /// Performs cleanup for the parser.
        /// </summary>
        public static void Close()
        {
            // Clean up devices
            foreach (XboxDevice device in pcapIds.Values)
            {
                device.Close();
            }

            // Clear IDs list
            pcapIds.Clear();

            // Reset flags
            canHandleNewDevices = true;
        }
    }
}
