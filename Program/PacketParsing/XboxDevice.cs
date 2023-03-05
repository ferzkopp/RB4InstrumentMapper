using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    public enum MappingMode
    {
        ViGEmBus = 1,
        vJoy = 2
    }

    /// <summary>
    /// Interface for Xbox devices.
    /// </summary>
    class XboxDevice
    {
        public static MappingMode MapperMode;

        /// <summary>
        /// Mapper interface to use.
        /// </summary>
        private IDeviceMapper deviceMapper;

        /// <summary>
        /// Creates a new XboxDevice with the given device ID and parsing mode.
        /// </summary>
        public XboxDevice()
        {
            switch (MapperMode)
            {
                case MappingMode.ViGEmBus:
                    deviceMapper = new VigemMapper();
                    break;

                case MappingMode.vJoy:
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
            if (!CommandHeader.TryParse(commandData, out var header, out int bytesRead))
            {
                return;
            }
            commandData = commandData.Slice(bytesRead);

            switch (header.CommandId)
            {
                case CommandHeader.Command.Input:
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