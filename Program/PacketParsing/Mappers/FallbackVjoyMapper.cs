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
        public FallbackVjoyMapper() : base()
        {
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        protected override XboxResult OnPacketReceived(CommandId command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case CommandId.Input:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        /// <summary>
        /// Parses an input report.
        /// </summary>
        public unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length == sizeof(GuitarInput) && MemoryMarshal.TryRead(data, out GuitarInput guitarReport))
            {
                GuitarVjoyMapper.HandleReport(ref state, guitarReport);
            }
            else if (data.Length == sizeof(DrumInput) && MemoryMarshal.TryRead(data, out DrumInput drumReport))
            {
                DrumsVjoyMapper.HandleReport(ref state, drumReport);
            }
#if DEBUG
            else if (data.Length == sizeof(GamepadInput) && MemoryMarshal.TryRead(data, out GamepadInput gamepadReport))
            {
                GamepadVjoyMapper.HandleReport(ref state, gamepadReport);
            }
#endif
            else
            {
                // Not handled
                return XboxResult.Success;
            }

            // Send data
            VjoyClient.UpdateDevice(deviceId, ref state);
            return XboxResult.Success;
        }
    }
}
