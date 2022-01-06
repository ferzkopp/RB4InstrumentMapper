using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Event handler for AppDomain.CurrentDomain.UnhandledException.
        /// </summary>
        /// <remarks>
        /// Logs the exception info to a file and prompts the user with the exception message.
        /// </remarks>
        public static void App_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            // The unhandled exception that fired off the event
            Exception unhandledException = args.ExceptionObject as Exception;

            // String representation of exception info
            string exceptionString = unhandledException.ToString();

            // Index to substring with to create exceptionMessage
            int removeIndex = exceptionString.IndexOf(Environment.NewLine);
            // First line of exceptionString (can't use Split since \n isn't a valid char)
            // Not using Exception.Message since it doesn't contain the exception type
            string exceptionMessage = exceptionString.Substring(0, removeIndex);

            try // Use an alternate message if log can't be written due to an exception
            {
                // User's Documents folder
                string docsFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                // Documents\RB4InstrumentMapper\Logs
                string logFolderPath = Path.Combine(docsFolder, "RB4InstrumentMapper\\Logs");
                if (!Directory.Exists(logFolderPath))
                {
                    // Create if it doesn't exist
                    Directory.CreateDirectory(logFolderPath);
                }

                // Current date/time
                DateTime currentTime = DateTime.Now;
                // Date to append to the log file name
                string fileDateTime = currentTime.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
                // Log file name with date appended
                string logFile = $"log_{fileDateTime}.txt";
                // Documents\RB4InstrumentMapper\Logs\log_<date>.txt
                string logFilePath = Path.Combine(logFolderPath, logFile);

                // Write to log file
                using (StreamWriter errorLog = File.AppendText(logFilePath))
                {
                    // Message to write to the log
                    StringBuilder message = new StringBuilder();

                    // Current date and time, formatted in Yeat/Month/Date Hour:Minute:Second with the invariant culture
                    string logDateTime = currentTime.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                    message.AppendLine($"[{logDateTime}] ERROR:");
                    message.AppendLine("------------------------------");
                    message.AppendLine(exceptionString);
                    message.AppendLine("------------------------------");

                    errorLog.Write(message.ToString());
                }

                // Prompt user
                MessageBoxResult result = MessageBox.Show(
                    $"An unhandled error has occured:\n\n{exceptionMessage}\n\nA log of the error has been created, do you want to open it?",
                    "Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error
                );
                // If user requested to, open the log
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(logFilePath);
                }
            }
            catch (Exception e)
            {
                // String representation of exception info
                string fileExceptionString = e.ToString();
                // Index to substring with to create exceptionMessage
                int fileExRemoveIndex = fileExceptionString.IndexOf(Environment.NewLine);
                // First line of exceptionString (can't use Split since \n isn't a valid char)
                // Not using Exception.Message since it doesn't contain the exception type
                string fileExceptionMessage = fileExceptionString.Substring(0, fileExRemoveIndex);

                // Alternate prompt indicating log wasn't able to be created
                MessageBox.Show(
                    $"An unhandled error has occured:\n\n{exceptionMessage}\n\nAn attempt to log the error was made, but failed:\n\n{fileExceptionMessage}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            // Close program
            MessageBox.Show(
                "The program will now shut down.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Application.Current.Shutdown();
        }
    }
}
