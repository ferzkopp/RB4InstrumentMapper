using System;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps drumkit inputs to a ViGEmBus device.
    /// </summary>
    internal class DrumsVigemMapper : VigemMapper
    {
        public DrumsVigemMapper(XboxClient client)
            : base(client)
        {
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxDrumInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        // Constants for the d-pad masks
        private const int yellowBit = 0x01;
        private const int blueBit = 0x02;

        // The previous state of the yellow/blue cymbals
        private int previousDpadCymbals;
        // The current state of the d-pad mask from the hit yellow/blue cymbals
        private int dpadMask;

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxDrumInput drumReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, drumReport, ref previousDpadCymbals, ref dpadMask);
            return SubmitReport();
        }

        /// <summary>
        /// Maps drumkit input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in XboxDrumInput report, ref int previousDpadCymbals, ref int dpadMask)
        {
            // Menu and Options
            var buttons = (XboxGamepadButton)report.Buttons;
            device.SetButtonState(Xbox360Button.Start, (buttons & XboxGamepadButton.Menu) != 0);
            device.SetButtonState(Xbox360Button.Back, (buttons & XboxGamepadButton.Options) != 0);

            // Dpad
            device.SetButtonState(Xbox360Button.Up, (buttons & XboxGamepadButton.DpadUp) != 0);
            device.SetButtonState(Xbox360Button.Down, (buttons & XboxGamepadButton.DpadDown) != 0);
            device.SetButtonState(Xbox360Button.Left, (buttons & XboxGamepadButton.DpadLeft) != 0);
            device.SetButtonState(Xbox360Button.Right, (buttons & XboxGamepadButton.DpadRight) != 0);

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

            device.SetButtonState(Xbox360Button.Up, ((dpadMask & yellowBit) != 0) || ((buttons & XboxGamepadButton.DpadUp) != 0));
            device.SetButtonState(Xbox360Button.Down, ((dpadMask & blueBit) != 0) || ((buttons & XboxGamepadButton.DpadDown) != 0));

            // Color flags
            device.SetButtonState(Xbox360Button.B, (redPad != 0) || ((buttons & XboxGamepadButton.B) != 0));
            device.SetButtonState(Xbox360Button.Y, ((yellowPad | yellowCym) != 0) || ((buttons & XboxGamepadButton.Y) != 0));
            device.SetButtonState(Xbox360Button.X, ((bluePad | blueCym) != 0) || ((buttons & XboxGamepadButton.X) != 0));
            device.SetButtonState(Xbox360Button.A, ((greenPad | greenCym) != 0) || ((buttons & XboxGamepadButton.A) != 0));

            // Pad flag
            device.SetButtonState(Xbox360Button.RightThumb,
                (redPad | yellowPad | bluePad | greenPad) != 0);
            // Cymbal flag
            device.SetButtonState(Xbox360Button.RightShoulder,
                (yellowCym | blueCym | greenCym) != 0);

            // Pedals
            device.SetButtonState(Xbox360Button.LeftShoulder,
                (report.Buttons & (ushort)XboxDrumButton.KickOne) != 0);
            device.SetButtonState(Xbox360Button.LeftThumb,
                (report.Buttons & (ushort)XboxDrumButton.KickTwo) != 0);

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
        }

        private static short ByteToVelocity(byte value)
        {
            // Scale the value to fill the byte
            value = (byte)(value * 0x11);

            // Bitwise invert to flip the value, then shift down one to exclude the sign bit
            int scaled = ~value.ScaleToUInt16();
            return (short)(scaled >> 1);
        }

        private static short ByteToVelocityNegative(byte value)
        {
            // Scale the value to fill the byte
            value = (byte)(value * 0x11);

            // Bitwise invert to flip the value, then shift down one to exclude the sign bit, then add our own
            int scaled = ~value.ScaleToUInt16();
            return (short)((scaled >> 1) | 0x8000);
        }
    }
}
