using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Provides functionality for logging.
    /// </summary>
    public static class LogUtils
    {
        /// <summary>
        /// The path to the folder to write logs to.
        /// </summary>
        /// <remarks>
        /// Currently %USERPROFILE%\Documents\RB4InstrumentMapper\Logs
        /// </remarks>
        public static readonly string LogFolderPath = Path.Combine(
            System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            $"RB4InstrumentMapper\\Logs"
        );

        /// <summary>
        /// The path to the folder to write packet logs to.
        /// </summary>
        /// <remarks>
        /// Currently %USERPROFILE%\Documents\RB4InstrumentMapper\Logs
        /// </remarks>
        public static readonly string PacketLogFolderPath = Path.Combine(
            System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            $"RB4InstrumentMapper\\PacketLogs"
        );

        /// <summary>
        /// Create a log file stream.
        /// </summary>
        public static StreamWriter CreateLogStream()
        {
            return CreateFileStream(LogFolderPath);
        }

        /// <summary>
        /// Create a packet log file stream.
        /// </summary>
        public static StreamWriter CreatePacketLogStream()
        {
            return CreateFileStream(PacketLogFolderPath);
        }

        /// <summary>
        /// Create a file stream in the specified folder.
        /// </summary>
        /// <param name="folderPath">
        /// The folder to create the file in.
        /// </param>
        static StreamWriter CreateFileStream(string folderPath)
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
            return ex?.ToString().Split('\n')[0];
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
        /// <param name="addtlInfo">
        /// Additional info to log after the stack trace.
        /// </param>
        public static void WriteException(this StreamWriter stream, Exception ex, string addtlInfo = null)
        {
            // Current date and time, formatted in Year/Month/Date Hour:Minute:Second with the invariant culture
            string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            // Log
            stream.WriteLine($"[{timestamp}] EXCEPTION:");
            stream.WriteLine("------------------------------");
            stream.WriteLine(ex.ToString());
            // Prevent writing an empty line if additional info is not provided
            if (addtlInfo != null)
            {
                stream.WriteLine(addtlInfo);
            }
            stream.WriteLine("------------------------------");
        }
    }
}
