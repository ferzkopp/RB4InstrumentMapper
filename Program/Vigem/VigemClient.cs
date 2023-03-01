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

        public static bool TryInitialize()
        {
            if (client != null)
                return true;

            try
            {
                client = new ViGEmClient();
                return true;
            }
            catch
            {
                client = null;
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

            return client.CreateXbox360Controller(0x1BAD, 0x0719);
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