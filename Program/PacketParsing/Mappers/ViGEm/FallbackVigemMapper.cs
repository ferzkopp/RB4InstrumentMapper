using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// The ViGEmBus mapper used when device type could not be determined. Maps based on report length.
    /// </summary>
    internal class FallbackVigemMapper : VigemMapper
    {
        public FallbackVigemMapper(XboxClient client)
            : base(client)
        {
        }

        protected override unsafe XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxGuitarInput.CommandId:
                // These have the same value
                // case DrumInput.CommandId:
                // #if DEBUG
                // case GamepadInput.CommandId:
                // #endif
                    return ParseInput(data);

                case XboxGHLGuitarInput.CommandId:
                    // Deliberately limit to the exact size
                    if (data.Length != sizeof(XboxGHLGuitarInput) || !MemoryMarshal.TryRead(data, out XboxGHLGuitarInput guitarReport))
                        return XboxResult.InvalidMessage;

                    GHLGuitarVigemMapper.HandleReport(device, guitarReport);
                    return SubmitReport();

                default:
                    return XboxResult.Success;
            }
        }

        // The previous state of the yellow/blue cymbals
        private int previousDpadCymbals;
        // The current state of the d-pad mask from the hit yellow/blue cymbals
        private int dpadMask;

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length == sizeof(XboxGuitarInput) && MemoryMarshal.TryRead(data, out XboxGuitarInput guitarReport))
            {
                GuitarVigemMapper.HandleReport(device, guitarReport);
            }
            else if (data.Length == sizeof(XboxDrumInput) && MemoryMarshal.TryRead(data, out XboxDrumInput drumReport))
            {
                DrumsVigemMapper.HandleReport(device, drumReport, ref previousDpadCymbals, ref dpadMask);
            }
#if DEBUG
            else if (data.Length == sizeof(XboxGamepadInput) && MemoryMarshal.TryRead(data, out XboxGamepadInput gamepadReport))
            {
                GamepadVigemMapper.HandleReport(device, gamepadReport);
            }
#endif
            else
            {
                // Not handled
                return XboxResult.Success;
            }

            return SubmitReport();
        }
    }
}
