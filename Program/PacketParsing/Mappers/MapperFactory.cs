using System;
using System.Collections.Generic;
using RB4InstrumentMapper.Vigem;
using RB4InstrumentMapper.Vjoy;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Creates a device mapper for a client.
    /// </summary>
    internal static class MapperFactory
    {
        private delegate DeviceMapper CreateMapper(XboxClient client);

        // Device interface GUIDs to check when getting the device mapper
        private static readonly Dictionary<Guid, CreateMapper> guidToMapper = new Dictionary<Guid, CreateMapper>()
        {
            { XboxDeviceGuids.MadCatzGuitar,  GetGuitarMapper },
            { XboxDeviceGuids.PdpGuitar,      GetGuitarMapper },

            { XboxDeviceGuids.MadCatzDrumkit, GetDrumsMapper },
            { XboxDeviceGuids.PdpDrumkit,     GetDrumsMapper },
    
            { XboxDeviceGuids.ActivisionGuitarHeroLive, GetGHLGuitarMapper },

            { XboxDeviceGuids.MadCatzLegacyWireless, GetWirelessLegacyMapper },

            { XboxDeviceGuids.XboxGamepad,    GetGamepadMapper },
        };

        // Interface GUIDs to ignore when more than one supported interface is found
        private static readonly HashSet<Guid> conflictIgnoredIds = new HashSet<Guid>()
        {
            // GHL guitars list both a unique interface and the gamepad interface
            XboxDeviceGuids.XboxGamepad,
        };

        public static DeviceMapper GetMapper(XboxClient client)
        {
            // Get unique interface GUID
            var interfaceGuids = client.Descriptor.InterfaceGuids;
            Guid interfaceGuid = default;
            foreach (var guid in interfaceGuids)
            {
                if (!guidToMapper.ContainsKey(guid))
                    continue;

                if (interfaceGuid != default)
                {
                    // Ignore IDs known to have conflicts
                    if (conflictIgnoredIds.Contains(guid))
                        continue;

                    PacketLogging.PrintMessage($"More than one recognized interface found! Cannot get specific mapper, device will not be mapped.");
                    PacketLogging.PrintMessage($"Consider filing a GitHub issue with the GUIDs below if this device should be supported:");
                    foreach (var guid2 in interfaceGuids)
                    {
                        PacketLogging.PrintMessage($"- {guid2}");
                    }
                    return null;
                }

                interfaceGuid = guid;
            }

            if (interfaceGuid == default)
            {
                PacketLogging.PrintMessage($"Could not find any supported interface IDs! Device will not be mapped.");
                PacketLogging.PrintMessage($"Consider filing a GitHub issue with the GUIDs below if this device should be supported:");
                foreach (var guid2 in interfaceGuids)
                {
                    PacketLogging.PrintMessage($"- {guid2}");
                }
                return null;
            }

            // Get mapper creation delegate for interface GUID
            if (!guidToMapper.TryGetValue(interfaceGuid, out var func))
            {
                PacketLogging.PrintMessage($"Could not get a specific mapper for interface {interfaceGuid}! Device will not be mapped.");
                PacketLogging.PrintMessage($"Consider filing a GitHub issue with the GUID above if this device should be supported.");
                return null;
            }

            try
            {
                var mapper = func(client);
                mapper.EnableInputs(client.Parent.InputsEnabled);
                return mapper;
            }
            catch (Exception ex)
            {
                PacketLogging.PrintException("Failed to create mapper for device!", ex);
                return null;
            }
        }

        private static DeviceMapper GetMapper(XboxClient client, CreateMapper createVigem, CreateMapper createVjoy)
        {
            DeviceMapper mapper;
            bool devicesAvailable;

            var mode = client.Parent.MappingMode;
            switch (mode)
            {
                case MappingMode.ViGEmBus:
                    mapper = VigemClient.AreDevicesAvailable ? createVigem(client) : null;
                    devicesAvailable = VigemClient.AreDevicesAvailable;
                    break;

                case MappingMode.vJoy:
                    mapper = VjoyClient.AreDevicesAvailable ? createVjoy(client) : null;
                    devicesAvailable = VjoyClient.AreDevicesAvailable;
                    break;

                default:
                    throw new NotImplementedException($"Unhandled mapping mode {mode}!");
            }

            if (mapper != null)
            {
                PacketLogging.PrintMessage($"Created new {mapper.GetType().Name}");
                if (!devicesAvailable)
                    PacketLogging.PrintMessage("Device limit reached, no new devices will be handled.");
            }

            return mapper;
        }

        public static DeviceMapper GetGamepadMapper(XboxClient client)
        {
#if DEBUG
            PacketLogging.PrintMessage("Warning: Gamepads are only supported in debug mode for testing purposes, they will not work in release builds.");
            return GetMapper(client,
                (c) => new GamepadVigemMapper(c),
                (c) => new GamepadVjoyMapper(c)
            );
#else
            // Don't log a message if connected over Pcap, as they're most likely not trying to use it with the program
            if (client.Parent.Backend == BackendType.Usb)
                PacketLogging.PrintMessage("Warning: Gamepads are not supported in RB4InstrumentMapper, as Windows already supports them.");
            return null;
#endif
        }

        public static DeviceMapper GetGuitarMapper(XboxClient client) => GetMapper(client,
            (c) => new GuitarVigemMapper(c),
            (c) => new GuitarVjoyMapper(c)
        );

        public static DeviceMapper GetDrumsMapper(XboxClient client) => GetMapper(client,
            (c) => new DrumsVigemMapper(c),
            (c) => new DrumsVjoyMapper(c)
        );

        public static DeviceMapper GetGHLGuitarMapper(XboxClient client) => GetMapper(client,
            (c) => new GHLGuitarVigemMapper(c),
            (c) => new GHLGuitarVjoyMapper(c)
        );

        public static DeviceMapper GetWirelessLegacyMapper(XboxClient client)
        {
            var mapper = new WirelessLegacyMapper(client);
            PacketLogging.PrintMessage($"Created new {nameof(WirelessLegacyMapper)} mapper");
            return mapper;
        }

        public static DeviceMapper GetFallbackMapper(XboxClient client) => GetMapper(client,
            (c) => new FallbackVigemMapper(c),
            (c) => new FallbackVjoyMapper(c)
        );
    }
}