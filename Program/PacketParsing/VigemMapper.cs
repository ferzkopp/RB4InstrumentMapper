using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Vigem;

namespace RB4InstrumentMapper.Parsing
{
    class VigemMapper : IDeviceMapper
    {
        /// <summary>
        /// The device to map to.
        /// </summary>
        private IXbox360Controller device;

        /// <summary>
        /// Whether or not feedback has been received to indicate that the device has connected.
        /// </summary>
        private bool deviceConnected = false;

        /// <summary>
        /// Creates a new VigemMapper.
        /// </summary>
        public VigemMapper()
        {
            device = VigemClient.CreateDevice();
            device.FeedbackReceived += ReceiveUserIndex;

            try
            {
                device.Connect();
            }
            catch (VigemNoFreeSlotException ex)
            {
                device = null;
                throw new ParseException("ViGEmBus device slots are full.", ex);
            }

            device.AutoSubmitReport = false;
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~VigemMapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Temporary event handler for logging the user index of a ViGEm device.
        /// </summary>
        void ReceiveUserIndex(object sender, Xbox360FeedbackReceivedEventArgs args)
        {
            // Device has connected
            deviceConnected = true;

            // Log the user index
            Console.WriteLine($"Created new ViGEmBus device with user index {args.LedNumber}");

            // Unregister the event handler
            (sender as IXbox360Controller).FeedbackReceived -= ReceiveUserIndex;
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        public void HandlePacket(CommandId command, ReadOnlySpan<byte> data)
        {
            if (device == null)
                throw new ObjectDisposedException(nameof(device));

            switch (command)
            {
                case CommandId.Input:
                    ParseInput(data);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Parses an input report.
        /// </summary>
        public unsafe void ParseInput(ReadOnlySpan<byte> data)
        {
            // Don't process if not connected
            if (!deviceConnected)
            {
                return;
            }

            if (data.Length == sizeof(GuitarInput) && MemoryMarshal.TryRead(data, out GuitarInput guitarReport))
            {
                ParseGuitar(guitarReport);
            }
            else if (data.Length == sizeof(DrumInput) && MemoryMarshal.TryRead(data, out DrumInput drumReport))
            {
                ParseDrums(drumReport);
            }
            else
            {
                // Report is not valid
                return;
            }

            // Send data
            device.SubmitReport();
        }

        /// <summary>
        /// Parses common button data from an input report.
        /// </summary>
        private void ParseCoreButtons(GamepadButton buttons)
        {
            // Menu
            device.SetButtonState(Xbox360Button.Start, (buttons & GamepadButton.Menu) != 0);
            // Options
            device.SetButtonState(Xbox360Button.Back, (buttons & GamepadButton.Options) != 0);

            // Dpad
            device.SetButtonState(Xbox360Button.Up, (buttons & GamepadButton.DpadUp) != 0);
            device.SetButtonState(Xbox360Button.Down, (buttons & GamepadButton.DpadDown) != 0);
            device.SetButtonState(Xbox360Button.Left, (buttons & GamepadButton.DpadLeft) != 0);
            device.SetButtonState(Xbox360Button.Right, (buttons & GamepadButton.DpadRight) != 0);

            // Other buttons are not mapped here since they may have specific uses
        }

        /// <summary>
        /// Parses guitar input data from an input report.
        /// </summary>
        private void ParseGuitar(GuitarInput report)
        {
            // Buttons
            ParseCoreButtons((GamepadButton)report.Buttons);

            // Frets
            device.SetButtonState(Xbox360Button.A, report.Green);
            device.SetButtonState(Xbox360Button.B, report.Red);
            device.SetButtonState(Xbox360Button.Y, report.Yellow);
            device.SetButtonState(Xbox360Button.X, report.Blue);
            device.SetButtonState(Xbox360Button.LeftShoulder, report.Orange);

            // Lower fret flag
            device.SetButtonState(Xbox360Button.LeftThumb, report.LowerFretFlag);

            // Whammy
            device.SetAxisValue(Xbox360Axis.RightThumbX, report.WhammyBar.ScaleToInt16());
            // Tilt
            device.SetAxisValue(Xbox360Axis.RightThumbY, report.Tilt.ScaleToInt16());
            // Pickup Switch
            device.SetSliderValue(Xbox360Slider.LeftTrigger, report.PickupSwitch);
        }

        // Constants for masks below
        const int yellowBit = 0x01;
        const int blueBit = 0x02;

        // The previous state of the yellow/blue cymbals
        int previousDpadCymbals;
        // The current state of the d-pad mask from the hit yellow/blue cymbals
        int dpadMask;

        /// <summary>
        /// Parses drums input data from an input report.
        /// </summary>
        private void ParseDrums(DrumInput report)
        {
            // Buttons
            var buttons = (GamepadButton)report.Buttons;
            ParseCoreButtons(buttons);

            // Pads and cymbals
            byte redPad    = report.RedPad;
            byte yellowPad = report.YellowPad;
            byte bluePad   = report.BluePad;
            byte greenPad  = report.GreenPad;

            byte yellowCym = report.YellowCymbal;
            byte blueCym   = report.BlueCymbal;
            byte greenCym  = report.GreenCymbal;

            // Yellow and blue cymbal trigger d-pad up and down respectively on the RB2/3 kit we're emulating
            // However, they only trigger one or the other, not both at the same time, so we need to mimic that
            int cymbalMask = (yellowCym != 0 ? yellowBit : 0) | (blueCym != 0 ? blueBit : 0);
            if (cymbalMask != previousDpadCymbals)
            {
                if (cymbalMask == 0)
                    dpadMask = 0;

                // This could probably be done more simply, but this works
                if (dpadMask != 0)
                {
                    // D-pad is already set
                    // Only remove the set value
                    if ((cymbalMask & yellowBit) == 0)
                        dpadMask &= ~yellowBit;
                    else if ((cymbalMask & blueBit) == 0)
                        dpadMask &= ~blueBit;
                }

                // Explicitly check this so that if the d-pad is cleared but the other cymbal is still active,
                // it will get set to that cymbal's d-pad
                if (dpadMask == 0)
                {
                    // D-pad is not set
                    // If both cymbals are hit at the same time, yellow takes priority
                    if ((cymbalMask & yellowBit) != 0)
                        dpadMask |= yellowBit;
                    else if ((cymbalMask & blueBit) != 0)
                        dpadMask |= blueBit;
                }

                previousDpadCymbals = cymbalMask;
            }

            device.SetButtonState(Xbox360Button.Up, ((dpadMask & yellowBit) != 0) || ((buttons & GamepadButton.DpadUp) != 0));
            device.SetButtonState(Xbox360Button.Down, ((dpadMask & blueBit) != 0) || ((buttons & GamepadButton.DpadDown) != 0));

            // Color flags
            device.SetButtonState(Xbox360Button.B, (redPad != 0) || ((buttons & GamepadButton.B) != 0));
            device.SetButtonState(Xbox360Button.Y, ((yellowPad | yellowCym) != 0) || ((buttons & GamepadButton.Y) != 0));
            device.SetButtonState(Xbox360Button.X, ((bluePad | blueCym) != 0) || ((buttons & GamepadButton.X) != 0));
            device.SetButtonState(Xbox360Button.A, ((greenPad | greenCym) != 0) || ((buttons & GamepadButton.A) != 0));

            // Pad flag
            device.SetButtonState(Xbox360Button.RightThumb,
                (redPad | yellowPad | bluePad | greenPad) != 0);
            // Cymbal flag
            device.SetButtonState(Xbox360Button.RightShoulder,
                (yellowCym | blueCym | greenCym) != 0);

            // Pedals
            device.SetButtonState(Xbox360Button.LeftShoulder, 
                (report.Buttons & (ushort)DrumButton.KickOne) != 0);
            device.SetButtonState(Xbox360Button.LeftThumb, 
                (report.Buttons & (ushort)DrumButton.KickTwo) != 0);

            // Velocities
            device.SetAxisValue(
                Xbox360Axis.LeftThumbX,
                ByteToVelocity(redPad)
            );
            device.SetAxisValue(
                Xbox360Axis.LeftThumbY,
                ByteToVelocityNegative((byte)(yellowPad | yellowCym))
            );
            device.SetAxisValue(
                Xbox360Axis.RightThumbX,
                ByteToVelocity((byte)(bluePad | blueCym))
            );
            device.SetAxisValue(
                Xbox360Axis.RightThumbY,
                ByteToVelocityNegative((byte)(greenPad | greenCym))
            );

            /// <summary>
            /// Scales a byte to a drums velocity value.
            /// </summary>
            short ByteToVelocity(byte value)
            {
                // Scale the value to fill the byte
                value = (byte)(value * 0x11);

                return (short)(
                    // Bitwise invert to flip the value, then shift down one to exclude the sign bit
                    (~value.ScaleToUInt16()) >> 1
                );
            }

            /// <summary>
            /// Scales a byte to a negative drums velocity value.
            /// </summary>
            short ByteToVelocityNegative(byte value)
            {
                // Scale the value to fill the byte
                value = (byte)(value * 0x11);

                return (short)(
                    // Bitwise invert to flip the value, then shift down one to exclude the sign bit, then add our own
                    ((~value.ScaleToUInt16()) >> 1) | 0x8000
                );
            }
        }

        /// <summary>
        /// Performs cleanup for the object.
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
                // Reset report
                try { device?.ResetReport(); } catch {}
                try { device?.SubmitReport(); } catch {}

                // Disconnect device
                try { device?.Disconnect(); } catch {}
                device = null;
            }
        }
    }
}
