using System;
using System.Runtime.InteropServices;
using RB4InstrumentMapper.Vjoy;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// A mapper that maps to a vJoy device.
    /// </summary>
    internal abstract class VjoyMapper : IDeviceMapper
    {
        public bool MapGuideButton { get; set; } = false;

        protected vJoy.JoystickState state = new vJoy.JoystickState();
        protected uint deviceId = 0;

        public VjoyMapper()
        {
            deviceId = VjoyClient.GetNextAvailableID();
            if (deviceId == 0)
            {
                throw new VjoyException("No vJoy devices are available.");
            }

            if (!VjoyClient.AcquireDevice(deviceId))
            {
                throw new VjoyException($"Could not claim vJoy device {deviceId}.");
            }

            state.bDevice = (byte)deviceId;
            Console.WriteLine($"Acquired vJoy device with ID of {deviceId}");
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~VjoyMapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        public XboxResult HandlePacket(CommandId command, ReadOnlySpan<byte> data)
        {
            if (deviceId == 0)
                throw new ObjectDisposedException("this");

            return OnPacketReceived(command, data);
        }

        protected abstract XboxResult OnPacketReceived(CommandId command, ReadOnlySpan<byte> data);

        public XboxResult HandleKeystroke(Keystroke key)
        {
            if (key.Keycode == KeyCode.LeftWindows && MapGuideButton)
            {
                state.SetButton(VjoyButton.Fourteen, key.Pressed);
                VjoyClient.UpdateDevice(deviceId, ref state);
            }

            return XboxResult.Success;
        }

        // vJoy axes range from 0x0000 to 0x8000, but are exposed as full ints for some reason
        protected static void SetAxis(ref int axisField, byte value)
        {
            axisField = (value * 0x0101) >> 1;
        }

        protected static void SetAxis(ref int axisField, short value)
        {
            axisField = ((ushort)value ^ 0x8000) >> 1;
        }

        protected static void SetAxisInverted(ref int axisField, short value)
        {
            axisField = 0x8000 - (((ushort)value ^ 0x8000) >> 1);
        }

        /// <summary>
        /// Parses the state of the d-pad.
        /// </summary>
        protected static void ParseDpad(ref vJoy.JoystickState state, GamepadButton buttons)
        {
            VjoyPoV direction;
            if ((buttons & GamepadButton.DpadUp) != 0)
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    direction = VjoyPoV.UpLeft;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    direction = VjoyPoV.UpRight;
                }
                else
                {
                    direction = VjoyPoV.Up;
                }
            }
            else if ((buttons & GamepadButton.DpadDown) != 0)
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    direction = VjoyPoV.DownLeft;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    direction = VjoyPoV.DownRight;
                }
                else
                {
                    direction = VjoyPoV.Down;
                }
            }
            else
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    direction = VjoyPoV.Left;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    direction = VjoyPoV.Right;
                }
                else
                {
                    direction = VjoyPoV.Neutral;
                }
            }

            state.bHats = (uint)direction;
        }

        /// <summary>
        /// Performs cleanup for the vJoy mapper.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Reset report
            state.Reset();
            VjoyClient.UpdateDevice(deviceId, ref state);

            // Free device
            VjoyClient.ReleaseDevice(deviceId);
            deviceId = 0;
        }
    }
}
