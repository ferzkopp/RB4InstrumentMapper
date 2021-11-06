using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Linq;

// Adds a Debug/Output Console to WPF application
// - XAML: <TextBox Name="messageConsole" />
// - MainWindow(): TextBoxOutputter.RedirectConsoleToTextBox(messageConsole);

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Adds a Debug/Output Console to WPF application
    /// </summary>
    /// <remarks>
    /// https://social.technet.microsoft.com/wiki/contents/articles/12347.wpfhowto-add-a-debugoutput-console-to-your-application.aspx
    /// </remarks>
    public class TextBoxConsole : TextWriter
    {
        /// <summary>
        /// Default maximum number of lines to keep in console.
        /// </summary>
        private const int DefaultMaxNumberLines = 100;

        /// <summary>
        /// Line split characters.
        /// </summary>
        private static char[] newlineChars = Environment.NewLine.ToCharArray();

        /// <summary>
        /// Cache for current line.
        /// </summary>
        private static StringBuilder currentLineCache = null;

        /// <summary>
        /// Cache of the visible text.
        /// </summary>
        private static FixedSizeConcurrentQueue<string> visibleTextCache = null;

        /// <summary>
        /// Text box handle that displays text
        /// </summary>
        private TextBox textBox = null;

        /// <summary>
        /// Display lines in reverse order (newest first) if set. Defaults to true.
        /// </summary>
        private bool displayLinesInReverseOrder = true;

        /// <summary>
        /// Display timestamp for each line. Defaults to true.
        /// </summary>
        private bool displayLinesWithTimestamp = true;

        /// <summary>
        /// Connect Console output given text box.
        /// </summary>
        /// <param name="textBox">Text box to receive console output.</param>
        /// <param name="maxNumberOfLines">Maximum number of lines to display in the text box. Defaults to 100.</param>
        /// <param name="displayLinesInReverseOrder">Display lines in reverse order (newest first). Defaults to true.</param>
        /// <param name="displayLinesWithTimestamp">Display timestamp for each line. Defaults to true.</param>
        public static void RedirectConsoleToTextBox(
            TextBox textBox,
            int maxNumberOfLines = DefaultMaxNumberLines,
            bool displayLinesInReverseOrder = true,
            bool displayLinesWithTimestamp = true)
        {
            TextBoxConsole textBoxOutputter = new TextBoxConsole(textBox, displayLinesInReverseOrder, displayLinesWithTimestamp);
            Console.SetOut(textBoxOutputter);
            currentLineCache = new StringBuilder();
            visibleTextCache = new FixedSizeConcurrentQueue<string>(maxNumberOfLines);
        }

        /// <summary>
        /// Create a TextBoxOutputter instance
        /// </summary>
        /// <param name="output">Target text box</param>
        /// <param name="reverse"> Reverse text.</param>
        public TextBoxConsole(TextBox output, bool reverse, bool timestamp)
        {
            textBox = output;
            displayLinesInReverseOrder = reverse;
            displayLinesWithTimestamp = timestamp;
        }

        /// <summary>
        /// Write text to outputter
        /// </summary>
        /// <param name="value"></param>
        public override void Write(char value)
        {
            base.Write(value);
            if (textBox != null &&
                currentLineCache != null &&
                visibleTextCache != null)
            {
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (newlineChars.Contains(value))
                    {
                        // Newline
                        if (currentLineCache.Length > 0)
                        {
                            // Store line in text cache and flush it
                            string newText = (displayLinesWithTimestamp) ? "[" + DateTime.Now.ToString("s") + "] " : string.Empty;
                            newText += currentLineCache.ToString();
                            visibleTextCache.Enqueue(newText);
                            currentLineCache.Clear();

                            // Display text cache
                            if (displayLinesInReverseOrder)
                            {
                                textBox.Text = string.Join(Environment.NewLine, visibleTextCache.ToArray().Reverse());
                            }
                            else
                            {
                                textBox.Text = string.Join(Environment.NewLine, visibleTextCache.ToArray());
                            }
                        }
                    }
                    else
                    {
                        // Collect characters in line
                        currentLineCache.Append(value);
                    }
                }));
            }
        }

        /// <summary>
        /// Set encoding of outputter
        /// </summary>
        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }
}
