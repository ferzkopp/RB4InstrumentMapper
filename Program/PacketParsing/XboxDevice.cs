using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Interface for Xbox devices.
    /// </summary>
    class XboxDevice
    {
        /// <summary>
        /// Mapper interface to use.
        /// </summary>
        private IDeviceMapper deviceMapper;

        // Lock off parameterless constructor
        private XboxDevice() {}

        /// <summary>
        /// Creates a new XboxDevice with the given device ID and parsing mode.
        /// </summary>
        public XboxDevice(ParsingMode parseMode)
        {
            switch (parseMode)
            {
                case ParsingMode.ViGEmBus:
                    deviceMapper = new VigemMapper();
                    break;

                case ParsingMode.vJoy:
                    deviceMapper = new VjoyMapper();
                    break;
            }
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~XboxDevice()
        {
            Close();
        }

        /// <summary>
        /// Parses command data from a packet.
        /// </summary>
        public unsafe void ParseCommand(ReadOnlySpan<byte> commandData)
        {
            if (!MemoryMarshal.TryRead(commandData, out CommandHeader header))
            {
                return;
            }

            switch (header.CommandId)
            {
                case (byte)CommandHeader.Command.Input:
                    deviceMapper.ParseInput(header, commandData.Slice(sizeof(CommandHeader)));
                    break;

                default:
                    // Don't do anything with unrecognized command IDs
                    break;
            }
        }

        /// <summary>
        /// Performs cleanup for the device.
        /// </summary>
        public void Close()
        {
            deviceMapper?.Close();
            deviceMapper = null;
        }
    }
}