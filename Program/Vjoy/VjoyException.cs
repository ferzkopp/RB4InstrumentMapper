using System;

namespace RB4InstrumentMapper.Vjoy
{
    /// <summary>
    /// A vJoy exception.
    /// </summary>
    class VjoyException : Exception
    {
        public VjoyException()
            : base() {}

        public VjoyException(string message)
            : base(message) {}

        public VjoyException(string message, Exception innerException)
            : base(message, innerException) {}

    }
}