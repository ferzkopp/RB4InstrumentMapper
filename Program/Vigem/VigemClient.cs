using System;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;

namespace RB4InstrumentMapper.Vigem
{
    /// <summary>
    /// Static vJoy client.
    /// </summary>
    public static class VigemClient
    {
        /// <summary>
        /// Static ViGEmBus client.
        /// </summary>
        private static ViGEmClient client;

        /// <summary>
        /// Whether or not the ViGEmBus client has been initialized.
        /// </summary>
        public static bool Initialized => client != null;

        /// <summary>
        /// Whether or not new devices can be created.
        /// </summary>
        public static bool AreDevicesAvailable => Initialized && canCreateDevices;
        private static bool canCreateDevices = false;

        public static bool TryInitialize()
        {
            if (client != null)
                return true;

            try
            {
                client = new ViGEmClient();
                canCreateDevices = true;
                return true;
            }
            catch
            {
                client = null;
                canCreateDevices = false;
                return false;
            }
        }

        /// <summary>
        /// Creates a new Xbox 360 device with the Xbox 360 Rock Band wireless instrument vendor/product IDs.
        /// </summary>
        public static IXbox360Controller CreateDevice()
        {
            if (!Initialized)
                throw new ObjectDisposedException(nameof(client), "ViGEmBus client is disposed or not initialized yet!");

            try
            {
                return client.CreateXbox360Controller(0x1BAD, 0x0719);
            }
            catch
            {
                canCreateDevices = false;
                throw;
            }
        }
        // Rock Band Guitar: USB\VID_1BAD&PID_0719&IG_00  XUSB\TYPE_00\SUB_86\VEN_1BAD\REV_0002
        // Rock Band Drums:  USB\VID_1BAD&PID_0719&IG_02  XUSB\TYPE_00\SUB_88\VEN_1BAD\REV_0002
        // If subtype ID specification through ViGEmBus becomes possible at some point,
        // the guitar should be subtype 6, and the drums should be subtype 8

        public static void Dispose()
        {
            client?.Dispose();
            client = null;
        }
    }
}