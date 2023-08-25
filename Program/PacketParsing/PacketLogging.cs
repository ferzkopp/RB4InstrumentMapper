using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Parsing
{
    internal static class PacketLogging
    {
        public static void LogPacket(XboxPacket packet)
        {
            if (!BackendSettings.LogPackets)
                return;

            string packetString = packet.ToString();
            LogMessage(packetString);
        }

        public static void LogMessage(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
            Logging.Packet_WriteLine(message);
        }

        public static void PrintMessage(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        public static void PrintVerboseError(string message)
        {
            // Always log errors to debug/log
            Debug.WriteLine(message);
            Logging.Main_WriteLine(message);
            if (!BackendSettings.PrintVerboseErrors)
                return;

            Console.WriteLine(message);
        }

        public static void PrintException(string message, Exception ex)
        {
            Debug.WriteLine(message);
            Debug.WriteLine(ex);
            Logging.Main_WriteException(ex, message);
            Console.WriteLine(message);
            Console.WriteLine(ex.GetFirstLine());
        }

        public static void PrintVerboseException(string message, Exception ex)
        {
            // Always log errors to debug/log
            Debug.WriteLine(message);
            Debug.WriteLine(ex);
            Logging.Main_WriteException(ex, message);

            if (!BackendSettings.PrintVerboseErrors)
                return;

            Console.WriteLine(message);
            Console.WriteLine(ex.GetFirstLine());
        }
    }
}