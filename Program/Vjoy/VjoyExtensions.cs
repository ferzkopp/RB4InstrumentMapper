using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Vjoy
{
    public static class VjoyExtensions
    {
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