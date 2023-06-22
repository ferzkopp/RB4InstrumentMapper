using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    public class DeviceCreationException : Exception
    {
        public DeviceCreationException() : base()
        {
        }

        public DeviceCreationException(string message) : base(message)
        {
        }

        public DeviceCreationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}