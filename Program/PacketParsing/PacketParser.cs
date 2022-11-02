using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RB4InstrumentMapper.Parsing;

// This is in the regular namespace to keep the other packet parsing stuff from bogging up
// auto-completions when the code in this file is the only code that needs to be referenced elsewhere 
namespace RB4InstrumentMapper
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
        public static unsafe void HandlePcapPacket(ReadOnlySpan<byte> data, ref ulong processedCount)
        {
            // Packet must be at least 30 bytes long, to contain both the receiver and command headers
            if (data.Length < (sizeof(ReceiverHeader) + sizeof(CommandHeader))
                || !MemoryMarshal.TryRead(data, out ReceiverHeader header))
            {
                return;
            }

            // Check if device ID has been encountered yet
            ulong deviceId = header.DeviceId;
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

            // Strip off receiver header and send the data to be parsed
            pcapIds[deviceId].ParseCommand(data.Slice(sizeof(ReceiverHeader)));
            processedCount++;
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
