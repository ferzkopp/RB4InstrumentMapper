using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

using Button = RB4InstrumentMapper.Parsing.VjoyStatic.Button;

namespace RB4InstrumentMapper.Parsing
{
    class VjoyMapper : IDeviceMapper
    {
        private vJoy.JoystickState state = new vJoy.JoystickState();
        private uint deviceId = 0;

        private byte prevInputSeqCount = 0xFF;

        /// <summary>
        /// Creates a new VjoyMapper.
        /// </summary>
        public VjoyMapper()
        {
            deviceId = VjoyStatic.ClaimNextAvailableDevice();
            state.bDevice = (byte)deviceId;
            Console.WriteLine($"Acquired vJoy device with ID of {deviceId}");
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~VjoyMapper()
        {
            Close();
        }

        /// <summary>
        /// Parses an input report.
        /// </summary>
        public unsafe void ParseInput(CommandHeader header, ReadOnlySpan<byte> data)
        {
            // Ensure lengths match
            if (header.DataLength != data.Length)
            {
                // This is probably a bug, emit a debug message
                Debug.Fail($"Command header length does not match buffer length! Header: {header.DataLength}  Buffer: {data.Length}");
                return;
            }

            // Don't parse the same report twice
            if (header.SequenceCount == prevInputSeqCount)
            {
                return;
            }

            header.SequenceCount = prevInputSeqCount;

            int length = header.DataLength;
            if (length == sizeof(GuitarInput) && MemoryMarshal.TryRead(data, out GuitarInput guitarReport))
            {
                ParseGuitar(guitarReport);
            }
            else if (length == sizeof(DrumInput) && MemoryMarshal.TryRead(data, out DrumInput drumReport))
            {
                ParseDrums(drumReport);
            }
            else
            {
                // Report is not valid
                return;
            }

            // Send data
            VjoyStatic.Client.UpdateVJD(deviceId, ref state);
        }

        /// <summary>
        /// Sets the state of the specified button.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetButton(uint button, bool condition)
        {
            if (condition)
            {
                state.Buttons |= button;
            }
            else
            {
                state.Buttons &= ~button;
            }
        }

        /// <summary>
        /// Parses common button data from an input report.
        /// </summary>
        private void ParseCoreButtons(GamepadButton buttons)
        {
            // Menu
            SetButton(Button.Fifteen, (buttons & GamepadButton.Menu) != 0);

            // Options
            SetButton(Button.Sixteen, (buttons & GamepadButton.Options) != 0);

            // D-pad to POV
            if ((buttons & GamepadButton.DpadUp) != 0)
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.UpLeft;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    state.bHats = VjoyStatic.PoV.UpRight;
                }
                else
                {
                    state.bHats = VjoyStatic.PoV.Up;
                }
            }
            else if ((buttons & GamepadButton.DpadDown) != 0)
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.DownLeft;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    state.bHats = VjoyStatic.PoV.DownRight;
                }
                else
                {
                    state.bHats = VjoyStatic.PoV.Down;
                }
            }
            else
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.Left;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    state.bHats = VjoyStatic.PoV.Right;
                }
                else
                {
                    state.bHats = VjoyStatic.PoV.Neutral;
                }
            }

            // Other buttons are not mapped here since they may have specific uses
        }

        /// <summary>
        /// Parses guitar input data from an input report.
        /// </summary>
        private void ParseGuitar(GuitarInput report)
        {
            // Buttons
            ParseCoreButtons((GamepadButton)report.Buttons);

            SetButton(Button.One, report.Green);
            SetButton(Button.Two, report.Red);
            SetButton(Button.Three, report.Yellow);
            SetButton(Button.Four, report.Blue);
            SetButton(Button.Five, report.Orange);

            // Whammy
            // Value ranges from 0 (not pressed) to 255 (fully pressed)
            state.AxisY = report.WhammyBar.ScaleToInt32();

            // Tilt
            // Value ranges from 0 to 255
            // It seems to have a threshold of around 0x70 though,
            // after a certain point values will get floored to 0
            state.AxisZ = report.Tilt.ScaleToInt32();

            // Pickup switch
            // Reported values are 0x00, 0x10, 0x20, 0x30, and 0x40 (ranges from 0 to 64)
            state.AxisX = report.PickupSwitch.ScaleToInt32();
        }

        /// <summary>
        /// Parses drums input data from an input report.
        /// </summary>
        private void ParseDrums(DrumInput report)
        {
            // Buttons
            var buttons = (GamepadButton)report.Buttons;
            ParseCoreButtons(buttons);

            // Face buttons
            SetButton(Button.Four, (buttons & GamepadButton.A) != 0);
            SetButton(Button.One, (buttons & GamepadButton.B) != 0);
            SetButton(Button.Three, (buttons & GamepadButton.X) != 0);
            SetButton(Button.Two, (buttons & GamepadButton.Y) != 0);

            // Pads
            SetButton(Button.One, report.RedPad != 0);
            SetButton(Button.Two, report.YellowPad != 0);
            SetButton(Button.Three, report.BluePad != 0);
            SetButton(Button.Four, report.GreenPad != 0);

            // Cymbals
            SetButton(Button.Six, report.YellowCymbal != 0);
            SetButton(Button.Seven, report.BlueCymbal != 0);
            SetButton(Button.Eight, report.GreenCymbal != 0);

            // Kick pedals
            SetButton(Button.Five, (report.Buttons & (ushort)DrumInput.Button.KickOne) != 0);
            SetButton(Button.Nine, (report.Buttons & (ushort)DrumInput.Button.KickTwo) != 0);
        }

        /// <summary>
        /// Performs cleanup for the vJoy mapper.
        /// </summary>
        public void Close()
        {
            // Reset report
            state.ResetState();
            VjoyStatic.Client.UpdateVJD(deviceId, ref state);

            // Free device
            VjoyStatic.ReleaseDevice(deviceId);
        }
    }
}
