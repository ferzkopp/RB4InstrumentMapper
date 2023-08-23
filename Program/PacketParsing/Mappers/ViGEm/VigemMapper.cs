using System;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Vigem;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// A mapper that maps to a ViGEmBus device.
    /// </summary>
    internal abstract class VigemMapper : DeviceMapper
    {
        /// <summary>
        /// The device to map to.
        /// </summary>
        protected IXbox360Controller device;

        /// <summary>
        /// Whether or not the emulated Xbox 360 controller has connected fully.
        /// </summary>
        protected bool deviceConnected = false;

        public VigemMapper(XboxClient client, bool mapGuide)
            : base(client, mapGuide)
        {
            device = VigemClient.CreateDevice();
            device.FeedbackReceived += DeviceConnected;
            device.Connect();
            device.AutoSubmitReport = false;
        }

        // Temporary event handler to ensure device connection
        private void DeviceConnected(object sender, Xbox360FeedbackReceivedEventArgs args)
        {
            // Device has connected
            deviceConnected = true;

            // Log the user index
            PacketLogging.PrintMessage($"Created new ViGEmBus device with user index {args.LedNumber}");

            // Unregister the event handler
            (sender as IXbox360Controller).FeedbackReceived -= DeviceConnected;
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        public override XboxResult HandleMessage(byte command, ReadOnlySpan<byte> data)
        {
            CheckDisposed();

            if (!deviceConnected)
                return XboxResult.Pending;

            return OnMessageReceived(command, data);
        }

        protected override void MapGuideButton(bool pressed)
        {
            device.SetButtonState(Xbox360Button.Guide, pressed);
            device.SubmitReport();
        }

        protected override void DisposeManagedResources()
        {
            if (device != null)
            {
                // Reset report
                try
                {
                    device.ResetReport(); 
                    device.SubmitReport();
                }
                catch { }

                // Disconnect device
                try
                {
                    device.Disconnect();
                }
                catch { }
            }

            device = null;
        }
    }
}