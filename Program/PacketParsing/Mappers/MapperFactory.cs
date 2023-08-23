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
        private delegate DeviceMapper CreateMapperForMode(MappingMode mode, XboxClient client, bool mapGuide);
        private delegate DeviceMapper CreateMapper(XboxClient client, bool mapGuide);

        // Device interface GUIDs to check when getting the device mapper
        private static readonly Dictionary<Guid, CreateMapperForMode> guidToMapper = new Dictionary<Guid, CreateMapperForMode>()
        {
            { XboxDeviceGuids.MadCatzGuitar,  GetGuitarMapper },
            { XboxDeviceGuids.PdpGuitar,      GetGuitarMapper },
            { XboxDeviceGuids.MadCatzDrumkit, GetDrumsMapper },
            { XboxDeviceGuids.PdpDrumkit,     GetDrumsMapper },

            { XboxDeviceGuids.MadCatzLegacyWireless, GetWirelessLegacyMapper },

#if DEBUG
            { XboxDeviceGuids.XboxGamepad,    GetGamepadMapper },
#endif
        };

        public static DeviceMapper GetMapper(IEnumerable<Guid> interfaceGuids, MappingMode mode,
            XboxClient client, bool mapGuide)
        {
            // Get unique interface GUID
            Guid interfaceGuid = default;
            foreach (var guid in interfaceGuids)
            {
                if (!guidToMapper.ContainsKey(guid))
                    continue;

                if (interfaceGuid != default)
                {
                    Console.WriteLine($"More than one unique interface GUID found! Cannot get specific mapper, using fallback mapper instead.");
                    Console.WriteLine($"Consider filing a GitHub issue with the GUIDs below so that this can be addressed:");
                    foreach (var guid2 in interfaceGuids)
                    {
                        Console.WriteLine($"- {guid2}");
                    }
                    return GetFallbackMapper(mode, client, mapGuide);
                }

                interfaceGuid = guid;
            }

            if (interfaceGuid == default)
            {
                Console.WriteLine($"Could not find interface GUID for device! Using fallback mapper instead.");
                Console.WriteLine($"Consider filing a GitHub issue with the GUIDs below so that this can be addressed:");
                foreach (var guid2 in interfaceGuids)
                {
                    Console.WriteLine($"- {guid2}");
                }
                return GetFallbackMapper(mode, client, mapGuide);
            }

            // Get mapper creation delegate for interface GUID
            if (!guidToMapper.TryGetValue(interfaceGuid, out var func))
            {
                Console.WriteLine($"Could not get a specific mapper for interface GUID {interfaceGuid}! Using fallback mapper instead.");
                Console.WriteLine($"Consider filing a GitHub issue with the GUID above so that it can be assigned the correct mapper.");
                return GetFallbackMapper(mode, client, mapGuide);
            }

            return func(mode, client, mapGuide);
        }

        private static DeviceMapper GetMapper(MappingMode mode, XboxClient client, bool mapGuide,
            CreateMapper createVigem, CreateMapper createVjoy)
        {
            try
            {
                DeviceMapper mapper;
                switch (mode)
                {
                    case MappingMode.ViGEmBus:
                        mapper = VigemClient.AreDevicesAvailable ? createVigem(client, mapGuide) : null;
                        break;
                    case MappingMode.vJoy:
                        mapper = VjoyClient.AreDevicesAvailable ? createVjoy(client, mapGuide) : null;
                        // Check if all devices have been used
                        if (mapper != null && !VjoyClient.AreDevicesAvailable)
                            Console.WriteLine("vJoy device limit reached, no new devices will be handled.");
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled mapping mode {mode}!");
                }

                if (mapper != null)
                    Console.WriteLine($"Created new {mapper.GetType()} mapper");
                return mapper;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create mapper for device: {ex.GetFirstLine()}");
                return null;
            }
        }

#if DEBUG
        public static DeviceMapper GetGamepadMapper(MappingMode mode, XboxClient client, bool mapGuide)
            => GetMapper(mode, client, mapGuide, VigemGamepadMapper, VjoyGamepadMapper);

        private static DeviceMapper VigemGamepadMapper(XboxClient client, bool mapGuide)
            => new GamepadVigemMapper(client, mapGuide);

        private static DeviceMapper VjoyGamepadMapper(XboxClient client, bool mapGuide)
            => new GamepadVigemMapper(client, mapGuide);
#endif

        public static DeviceMapper GetGuitarMapper(MappingMode mode, XboxClient client, bool mapGuide)
            => GetMapper(mode, client, mapGuide, VigemGuitarMapper, VjoyGuitarMapper);

        private static DeviceMapper VigemGuitarMapper(XboxClient client, bool mapGuide)
            => new GuitarVigemMapper(client, mapGuide);

        private static DeviceMapper VjoyGuitarMapper(XboxClient client, bool mapGuide)
            => new GuitarVigemMapper(client, mapGuide);

        public static DeviceMapper GetDrumsMapper(MappingMode mode, XboxClient client, bool mapGuide)
            => GetMapper(mode, client, mapGuide, VigemDrumsMapper, VjoyDrumsMapper);

        private static DeviceMapper VigemDrumsMapper(XboxClient client, bool mapGuide)
            => new DrumsVigemMapper(client, mapGuide);

        private static DeviceMapper VjoyDrumsMapper(XboxClient client, bool mapGuide)
            => new DrumsVigemMapper(client, mapGuide);

        public static DeviceMapper GetWirelessLegacyMapper(MappingMode mode, XboxClient client, bool mapGuide)
        {
            try
            {
                var mapper = new WirelessLegacyMapper(mode, client, mapGuide);
                Console.WriteLine($"Created new {nameof(WirelessLegacyMapper)} mapper");
                return mapper;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create mapper for device: {ex.GetFirstLine()}");
                return null;
            }
        }

        public static DeviceMapper GetFallbackMapper(MappingMode mode, XboxClient client, bool mapGuide)
            => GetMapper(mode, client, mapGuide, VigemFallbackMapper, VjoyFallbackMapper);

        private static DeviceMapper VigemFallbackMapper(XboxClient client, bool mapGuide)
            => new FallbackVigemMapper(client, mapGuide);

        private static DeviceMapper VjoyFallbackMapper(XboxClient client, bool mapGuide)
            => new FallbackVigemMapper(client, mapGuide);
    }
}