using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Provides functionality for logging.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// The file to log errors to.
        /// </summary>
        private static StreamWriter mainLog = null;

        /// <summary>
        /// Gets whether or not the main log exists.
        /// </summary>
        public static bool MainLogExists => mainLog != null;

        private static bool allowMainLogCreation = true;

        /// <summary>
        /// The current file to log packets to.
        /// </summary>
        private static StreamWriter packetLog = null;

        /// <summary>
        /// Gets whether or not a packet log exists.
        /// </summary>
        public static bool PacketLogExists => packetLog != null;

        /// <summary>
        /// The path to the folder to write logs to.
        /// </summary>
        /// <remarks>
        /// Currently %USERPROFILE%\Documents\RB4InstrumentMapper\Logs
        /// </remarks>
        public static readonly string LogFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RB4InstrumentMapper",
            "Logs"
        );

        /// <summary>
        /// The path to the folder to write packet logs to.
        /// </summary>
        /// <remarks>
        /// Currently %USERPROFILE%\Documents\RB4InstrumentMapper\PacketLogs
        /// </remarks>
        public static readonly string PacketLogFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RB4InstrumentMapper",
            "PacketLogs"
        );

        /// <summary>
        /// Creates a file stream in the specified folder.
        /// </summary>
        /// <param name="folderPath">
        /// The folder to create the file in.
        /// </param>
        private static StreamWriter CreateFileStream(string folderPath)
        {
            // Create logs folder if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string currentTimeString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            string filePath = Path.Combine(folderPath, $"log_{currentTimeString}.txt");

            try
            {
                return new StreamWriter(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't create log file {filePath}:");
                Console.WriteLine(ex.GetFirstLine());
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Creates the main log file.
        /// </summary>
        public static bool CreateMainLog()
        {
            if (!allowMainLogCreation || mainLog != null)
                return true;

            mainLog = CreateFileStream(LogFolderPath);
            if (mainLog == null)
            {
                // Log could not be created, don't allow creating it again to prevent console spam
                allowMainLogCreation = false;
                return false;
            }

            Console.WriteLine("Created main log file.");
            return true;
        }

        /// <summary>
        /// Creates a packet log file.
        /// </summary>
        public static bool CreatePacketLog()
        {
            if (packetLog != null)
                return true;

            packetLog = CreateFileStream(PacketLogFolderPath);
            if (packetLog == null)
                return false;

            Console.WriteLine("Created packet log file.");
            return true;
        }

        /// <summary>
        /// Writes a line to the log file.
        /// </summary>
        public static void Main_WriteLine(string text)
        {
            // Create log file if it hasn't been made yet
            CreateMainLog();

            Debug.WriteLine(text);
            mainLog?.WriteLine(GetMessageHeader(text));
        }

        /// <summary>
        /// Writes an exception, and any context, to the log.
        /// </summary>
        public static void Main_WriteException(Exception ex, string context = null)
        {
            // Create log file if it hasn't been made yet
            CreateMainLog();

            // Prevent writing an empty line if context is not provided
            if (context != null)
                Debug.WriteLine(context);
            Debug.WriteLine(ex.ToString());

            mainLog?.WriteException(ex, context);
        }

        public static void Packet_WriteLine(string text)
        {
            // Don't create log file if it hasn't been made yet
            // Packet log should be created manually
            packetLog?.WriteLine(GetMessageHeader(text));
        }

        /// <summary>
        /// Closes the main log file.
        /// </summary>
        public static void CloseMainLog()
        {
            mainLog?.Close();
            mainLog = null;
        }

        /// <summary>
        /// Closes the active packet log file.
        /// </summary>
        public static void ClosePacketLog()
        {
            packetLog?.Close();
            packetLog = null;
        }

        /// <summary>
        /// Closes all log files.
        /// </summary>
        public static void CloseAll()
        {
            CloseMainLog();
            ClosePacketLog();
        }

        // Extension method for getting the first line of Exception.ToString(),
        // since Exception.Message doesn't include the exception type
        /// <summary>
        /// Gets the first line of the <see cref="Exception.ToString()"/> method, to include the exception type in the message.
        /// </summary>
        /// <param name="ex">
        /// The exception being extended with this method.
        /// </param>
        public static string GetFirstLine(this Exception ex)
        {
            return ex.ToString().Split('\n')[0];
        }

        // Extension method for logging exceptions to streams in a customized manner
        /// <summary>
        /// Writes an exception to the stream in a particularly emphasized fashion.
        /// </summary>
        /// <param name="stream">
        /// The StreamWriter being extended with this method.
        /// </param>
        /// <param name="ex">
        /// The exception to log.
        /// </param>
        /// <param name="context">
        /// Additional context for the exception.
        /// </param>
        public static void WriteException(this StreamWriter stream, Exception ex, string context = null)
        {
            stream.WriteLine(GetMessageHeader("EXCEPTION"));
            stream.WriteLine("------------------------------");
            // Prevent writing an empty line if context is not provided
            if (context != null)
                stream.WriteLine(context);
            stream.WriteLine(ex.ToString());
            stream.WriteLine("------------------------------");
        }

        private static string GetMessageHeader(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            return $"[{timestamp}] {message}";
        }
    }
}
