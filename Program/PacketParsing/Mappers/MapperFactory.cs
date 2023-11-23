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
                return func(client);
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
            var mode = client.Parent.MappingMode;
            switch (mode)
            {
                case MappingMode.ViGEmBus:
                    mapper = VigemClient.AreDevicesAvailable ? createVigem(client) : null;
                    // Check if all devices have been used
                    if (mapper != null && !VigemClient.AreDevicesAvailable)
                        PacketLogging.PrintMessage("ViGEmBus device limit reached, no new devices will be handled.");
                    break;
                case MappingMode.vJoy:
                    mapper = VjoyClient.AreDevicesAvailable ? createVjoy(client) : null;
                    // Check if all devices have been used
                    if (mapper != null && !VjoyClient.AreDevicesAvailable)
                        PacketLogging.PrintMessage("vJoy device limit reached, no new devices will be handled.");
                    break;
                case MappingMode.NotSet:
                    throw new InvalidOperationException("Mapping mode is not set! Cannot create mapper.");
                default:
                    throw new NotImplementedException($"Unhandled mapping mode {mode}!");
            }

            if (mapper != null)
                PacketLogging.PrintMessage($"Created new {mapper.GetType().Name}");
            return mapper;
        }

        public static DeviceMapper GetGamepadMapper(XboxClient client)
        {
#if DEBUG
            PacketLogging.PrintMessage("Warning: Gamepads are only supported in debug mode for testing purposes, they will not work in release builds.");
            return GetMapper(client, VigemGamepadMapper, VjoyGamepadMapper);
#else
            // Don't log a message if connected over Pcap, as they're most likely not trying to use it with the program
            if (client.Parent.Backend == BackendType.Usb)
                PacketLogging.PrintMessage("Warning: Gamepads are not supported in RB4InstrumentMapper, as Windows already supports them.");
            return null;
#endif
        }

#if DEBUG
        private static DeviceMapper VigemGamepadMapper(XboxClient client)
            => new GamepadVigemMapper(client);

        private static DeviceMapper VjoyGamepadMapper(XboxClient client)
            => new GamepadVigemMapper(client);
#endif

        public static DeviceMapper GetGuitarMapper(XboxClient client)
            => GetMapper(client, VigemGuitarMapper, VjoyGuitarMapper);

        private static DeviceMapper VigemGuitarMapper(XboxClient client)
            => new GuitarVigemMapper(client);

        private static DeviceMapper VjoyGuitarMapper(XboxClient client)
            => new GuitarVigemMapper(client);

        public static DeviceMapper GetDrumsMapper(XboxClient client)
            => GetMapper(client, VigemDrumsMapper, VjoyDrumsMapper);

        private static DeviceMapper VigemDrumsMapper(XboxClient client)
            => new DrumsVigemMapper(client);

        private static DeviceMapper VjoyDrumsMapper(XboxClient client)
            => new DrumsVigemMapper(client);

        public static DeviceMapper GetGHLGuitarMapper(XboxClient client)
            => GetMapper(client, VigemGHLGuitarMapper, VjoyGHLGuitarMapper);

        private static DeviceMapper VigemGHLGuitarMapper(XboxClient client)
            => new GHLGuitarVigemMapper(client);

        private static DeviceMapper VjoyGHLGuitarMapper(XboxClient client)
            => new GHLGuitarVigemMapper(client);

        public static DeviceMapper GetWirelessLegacyMapper(XboxClient client)
        {
            var mapper = new WirelessLegacyMapper(client);
            PacketLogging.PrintMessage($"Created new {nameof(WirelessLegacyMapper)} mapper");
            return mapper;
        }

        public static DeviceMapper GetFallbackMapper(XboxClient client)
            => GetMapper(client, VigemFallbackMapper, VjoyFallbackMapper);

        private static DeviceMapper VigemFallbackMapper(XboxClient client)
            => new FallbackVigemMapper(client);

        private static DeviceMapper VjoyFallbackMapper(XboxClient client)
            => new FallbackVigemMapper(client);
    }
}