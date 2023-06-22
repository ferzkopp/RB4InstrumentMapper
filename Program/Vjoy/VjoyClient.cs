using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Vjoy
{
    /// <summary>
    /// Provides functionality for logging.
    /// </summary>
    public static class VjoyClient
    {
        private static readonly vJoy client = new vJoy();

        public static bool Enabled => client.vJoyEnabled();

        public static string Manufacturer => client.GetvJoyManufacturerString();
        public static string Product => client.GetvJoyProductString();
        public static string SerialNumber => client.GetvJoySerialNumberString();

        public static bool AreDevicesAvailable => Enabled && GetNextAvailableID() > 0;

        public static bool DriverMatch(out uint libraryVersion, out uint driverVersion)
        {
            libraryVersion = 0;
            driverVersion = 0;
            return client.DriverMatch(ref libraryVersion, ref driverVersion);
        }

        public static VjdStat GetDeviceStatus(uint deviceId) => client.GetVJDStatus(deviceId);

        public static bool IsDeviceAvailable(uint deviceId)
        {
            return (GetDeviceStatus(deviceId) == VjdStat.VJD_STAT_FREE) && IsDeviceCompatible(deviceId);
        }

        public static bool IsDeviceCompatible(uint deviceId)
        {
            // Check that the vJoy device is configured correctly
            int numButtons = client.GetVJDButtonNumber(deviceId);
            int numContPov = client.GetVJDContPovNumber(deviceId);
            bool xExists = client.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_X);
            bool yExists = client.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_Y);
            bool zExists = client.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_Z);

            if (numButtons >= 16 && numContPov >= 1 &&
                xExists && yExists && zExists
            )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the next available device ID.
        /// </summary>
        public static uint GetNextAvailableID()
        {
            // Get available devices
            for (uint deviceId = 1; deviceId <= 16; deviceId++)
            {
                // Ensure device is available
                if (client.GetVJDStatus(deviceId) == VjdStat.VJD_STAT_FREE && IsDeviceCompatible(deviceId))
                {
                    return deviceId;
                }
            }

            // No devices available
            return 0;
        }

        /// <summary>
        /// Acquires a vJoy device.
        /// </summary>
        public static bool AcquireDevice(uint deviceId)
        {
            return client.AcquireVJD(deviceId);
        }

        /// <summary>
        /// Releases a vJoy device.
        /// </summary>
        public static void ReleaseDevice(uint deviceId)
        {
            if (client.GetVJDStatus(deviceId) == VjdStat.VJD_STAT_OWN)
            {
                client.RelinquishVJD(deviceId);
            }
        }

        public static void FreeAllDevices()
        {
            for (uint i = 1; i <= 16; i++)
            {
                ReleaseDevice(i);
            }
        }

        /// <summary>
        /// Acquires a vJoy device.
        /// </summary>
        public static bool UpdateDevice(uint deviceId, ref vJoy.JoystickState state)
        {
            return client.UpdateVJD(deviceId, ref state);
        }
    }
}