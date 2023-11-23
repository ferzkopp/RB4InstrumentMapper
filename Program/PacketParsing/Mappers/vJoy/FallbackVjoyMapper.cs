using System;
using System.Runtime.InteropServices;
using RB4InstrumentMapper.Vjoy;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// The vJoy mapper used when device type could not be determined. Maps based on report length.
    /// </summary>
    internal class FallbackVjoyMapper : VjoyMapper
    {
        public FallbackVjoyMapper(XboxClient client)
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
                    if (data.Length != sizeof(XboxGHLGuitarInput) || !MemoryMarshal.TryRead(data, out XboxGHLGuitarInput guitarReport))
                        return XboxResult.InvalidMessage;

                    GHLGuitarVjoyMapper.HandleReport(ref state, guitarReport);
                    return XboxResult.Success;

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length == sizeof(XboxGuitarInput) && MemoryMarshal.TryRead(data, out XboxGuitarInput guitarReport))
            {
                GuitarVjoyMapper.HandleReport(ref state, guitarReport);
            }
            else if (data.Length == sizeof(XboxDrumInput) && MemoryMarshal.TryRead(data, out XboxDrumInput drumReport))
            {
                DrumsVjoyMapper.HandleReport(ref state, drumReport);
            }
#if DEBUG
            else if (data.Length == sizeof(XboxGamepadInput) && MemoryMarshal.TryRead(data, out XboxGamepadInput gamepadReport))
            {
                GamepadVjoyMapper.HandleReport(ref state, gamepadReport);
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
