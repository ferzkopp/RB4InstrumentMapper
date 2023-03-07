using System.Runtime.CompilerServices;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Vjoy
{
    public static class VjoyExtensions
    {
        /// <summary>
        /// Sets the state of the specified button.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetButton(this vJoy.JoystickState state, VjoyButton button, bool set)
        {
            if (set)
            {
                state.Buttons |= (uint)button;
            }
            else
            {
                state.Buttons &= (uint)~button;
            }
        }

        /// <summary>
        /// Resets the values of this state.
        /// </summary>
        public static void Reset(this vJoy.JoystickState state)
        {
            // Only reset the values we use
            state.Buttons = (uint)VjoyButton.None;
            state.bHats = (uint)VjoyPoV.Neutral;
            state.AxisX = 0;
            state.AxisY = 0;
            state.AxisZ = 0;
        }
    }
}