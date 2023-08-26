using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps devices connected to a RB4 wireless legacy adapter to virtual controllers.
    /// </summary>
    internal class WirelessLegacyMapper : DeviceMapper
    {
        // Mappers are not guaranteed to be created for each device, unknown subtypes will be ignored and have none
        private readonly Dictionary<byte, DeviceMapper> mappers = new Dictionary<byte, DeviceMapper>();

        public WirelessLegacyMapper(XboxClient client)
            : base(client)
        {
            client.SendMessage(XboxWirelessLegacyRequestDevices.RequestDevices);
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxWirelessLegacyInputHeader.CommandId:
                    return HandleInput(data);

                case XboxWirelessLegacyDeviceConnect.CommandId:
                    return HandleConnection(data);

                case XboxWirelessLegacyDeviceDisconnect.CommandId:
                    return HandleDisconnection(data);
            }

            return XboxResult.Success;
        }

        private unsafe XboxResult HandleInput(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(XboxWirelessLegacyInputHeader) ||
                !MemoryMarshal.TryRead(data, out XboxWirelessLegacyInputHeader header))
                return XboxResult.InvalidMessage;

            // Find the mapper for the given user index
            byte userIndex = header.UserIndex;
            if (!mappers.TryGetValue(userIndex, out var mapper))
            {
                PacketLogging.PrintVerboseError($"Missing mapper for wireless legacy user index {userIndex}!");
                return XboxResult.InvalidMessage;
            }

            data = data.Slice(sizeof(XboxWirelessLegacyInputHeader));
            return mapper?.HandleMessage(XboxWirelessLegacyInputHeader.CommandId, data) ?? XboxResult.Success;
        }

        private XboxResult HandleConnection(ReadOnlySpan<byte> data)
        {
            if (!XboxWirelessLegacyDeviceConnect.TryParse(data, out var connect))
                return XboxResult.InvalidMessage;

            // Find the mapper for the given user index
            byte userIndex = connect.UserIndex;
            if (mappers.TryGetValue(userIndex, out var mapper))
            {
                PacketLogging.PrintVerboseError($"Mapper already exists for legacy adapter user index {userIndex}! Overwriting.");
                mapper?.Dispose();
            }

            mappers[userIndex] = GetMapperForDevice(connect);
            return XboxResult.Success;
        }

        private unsafe XboxResult HandleDisconnection(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(XboxWirelessLegacyDeviceDisconnect) ||
                !MemoryMarshal.TryRead(data, out XboxWirelessLegacyDeviceDisconnect disconnect))
                return XboxResult.InvalidMessage;

            // Find the mapper for the given user index
            byte userIndex = disconnect.UserIndex;
            if (!mappers.TryGetValue(userIndex, out var mapper))
            {
                PacketLogging.PrintVerboseError($"Missing mapper for legacy adapter user index {userIndex}!");
                return XboxResult.InvalidMessage;
            }

            mapper?.Dispose();
            mappers.Remove(userIndex);
            return XboxResult.Success;
        }

        public override XboxResult HandleKeystroke(XboxKeystroke key)
        {
            foreach (var mapper in mappers.Values)
            {
                var result = mapper.HandleKeystroke(key);
                if (result != XboxResult.Success)
                    return result;
            }

            return XboxResult.Success;
        }

        // Handled by the override above
        protected override void MapGuideButton(bool pressed) { }

        private DeviceMapper GetMapperForDevice(XboxWirelessLegacyDeviceConnect connect)
        {
            var subtype = connect.DeviceSubtype;
            switch (subtype)
            {
#if DEBUG
                case XInputDeviceSubtype.Gamepad:
                    return MapperFactory.GetGamepadMapper(client);
#endif

                case XInputDeviceSubtype.Guitar:
                case XInputDeviceSubtype.GuitarAlternate:
                case XInputDeviceSubtype.GuitarBass:
                    return MapperFactory.GetGuitarMapper(client);

                case XInputDeviceSubtype.Drums:
                    return MapperFactory.GetGuitarMapper(client);

                default:
                    PacketLogging.PrintMessage($"User index {connect.UserIndex + 1} on the legacy adapter has an unsupported subtype ({subtype})!");
                    PacketLogging.PrintMessage("If you think it should be supported, restart capture with packet logging to a file enabled, go through all of the inputs, and create a GitHub issue with the log file attached.");
                    return null;
            }
        }

        protected override void DisposeManagedResources()
        {
            foreach (var mapper in mappers.Values)
            {
                mapper.Dispose();
            }

            mappers.Clear();
        }
    }
}