using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// A mapper which maps inputs from a physical device to a virtual controller.
    /// </summary>
    internal abstract class DeviceMapper : IDisposable
    {
        protected readonly XboxClient client;
        protected readonly bool mapGuideButton;

        protected bool disposed = false;
        protected bool inputsEnabled = true;

        /// <summary>
        /// Initializes a new device mapper with the given parent client,
        /// and option of whether or not to map the guide button.
        /// </summary>
        public DeviceMapper(XboxClient client)
        {
            this.client = client;

            mapGuideButton = client.Parent.MapGuideButton;
        }

        ~DeviceMapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        public virtual XboxResult HandleMessage(byte command, ReadOnlySpan<byte> data)
        {
            CheckDisposed();
            return OnMessageReceived(command, data);
        }

        /// <summary>
        /// Handles a keystroke message.
        /// </summary>
        public virtual XboxResult HandleKeystroke(XboxKeystroke key)
        {
            CheckDisposed();

            if (key.Keycode == XboxKeyCode.LeftWindows && mapGuideButton)
            {
                MapGuideButton(key.Pressed);
            }

            return XboxResult.Success;
        }

        protected abstract XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data);
        protected abstract void MapGuideButton(bool pressed);

        public abstract void ResetReport();

        public virtual void EnableInputs(bool enabled)
        {
            inputsEnabled = enabled;
            if (!enabled)
                ResetReport();
        }

        /// <summary>
        /// Disposes the mapper and any resources it uses.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeManagedResources();
            }

            DisposeUnmanagedResources();

            disposed = true;
        }

        protected void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("this");
        }

        protected virtual void DisposeManagedResources()
        {
        }

        protected virtual void DisposeUnmanagedResources()
        {
        }
    }
}