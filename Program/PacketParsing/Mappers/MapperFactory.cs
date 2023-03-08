using System;
using System.Collections.Generic;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Factory for device mappers.
    /// </summary>
    internal static class MapperFactory
    {
        // Device interface GUIDs to check when getting the device mapper
        private static Dictionary<Guid, Func<MappingMode, IDeviceMapper>> guidToMapper = new Dictionary<Guid, Func<MappingMode, IDeviceMapper>>()
        {
            { DeviceGuids.MadCatzGuitar, GetGuitarMapper },
            // { DeviceGuids.PdpGuitar, GetGuitarMapper },
            { DeviceGuids.MadCatzDrumkit, GetDrumsMapper },
            { DeviceGuids.PdpDrumkit, GetDrumsMapper },
        };

        // Non-unique interface GUIDs to ignore
        private static List<Guid> guidExclusionList = new List<Guid>()
        {
            DeviceGuids.XboxInputDevice,
            DeviceGuids.XboxNavigationController,
        };

        public static IDeviceMapper GetMapper(IReadOnlyList<Guid> interfaceGuids, MappingMode mode)
        {
            // Get unique interface GUID
            Guid interfaceGuid = default;
            foreach (var guid in interfaceGuids)
            {
                if (guidExclusionList.Contains(guid))
                    continue;

                if (interfaceGuid != default)
                {
                    Console.WriteLine($"More than one unique interface GUID found! Cannot get specific mapper, using fallback mapper instead.");
                    Console.WriteLine($"Consider filing a GitHub issue with the GUIDs below so that this can be addressed:");
                    foreach (var guid2 in interfaceGuids)
                    {
                        Console.WriteLine($"- {guid2}");
                    }
                    return GetFallbackMapper(mode);
                }

                interfaceGuid = guid;
            }

            // Get mapper creation delegate for interface GUID
            if (!guidToMapper.TryGetValue(interfaceGuid, out var func))
            {
                Console.WriteLine($"Could not get a specific mapper for interface GUID {interfaceGuid}! Using fallback mapper instead.");
                Console.WriteLine($"Consider filing a GitHub issue with the GUID above so that it can be assigned the correct mapper.");
                return GetFallbackMapper(mode);
            }

            return func(mode);
        }

        public static IDeviceMapper GetGuitarMapper(MappingMode mode)
        {
            Console.WriteLine($"Guitar found, creating new {mode} mapper...");
            switch (mode)
            {
                case MappingMode.ViGEmBus: return new GuitarVigemMapper();
                case MappingMode.vJoy: return new GuitarVjoyMapper();
                default: throw new Exception("Unhandled mapping mode!");
            }
        }

        public static IDeviceMapper GetDrumsMapper(MappingMode mode)
        {
            Console.WriteLine($"Drumkit found, creating new {mode} mapper...");
            switch (mode)
            {
                case MappingMode.ViGEmBus: return new DrumsVigemMapper();
                case MappingMode.vJoy: return new DrumsVjoyMapper();
                default: throw new Exception("Unhandled mapping mode!");
            }
        }

        public static IDeviceMapper GetFallbackMapper(MappingMode mode)
        {
            Console.WriteLine($"Creating new fallback {mode} mapper...");
            switch (mode)
            {
                case MappingMode.ViGEmBus: return new DrumsVigemMapper();
                case MappingMode.vJoy: return new DrumsVjoyMapper();
                default: throw new Exception("Unhandled mapping mode!");
            }
        }
    }
}