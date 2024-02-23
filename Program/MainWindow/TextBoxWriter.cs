using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// A text writer which writes to a WPF textbox.
    /// </summary>
    /// <remarks>
    /// https://social.technet.microsoft.com/wiki/contents/articles/12347.wpfhowto-add-a-debugoutput-console-to-your-application.aspx
    /// </remarks>
    public class TextBoxWriter : TextWriter
    {
        /// <summary>
        /// Default maximum number of lines to keep in console.
        /// </summary>
        private const int DefaultMaxNumberLines = 100;

        /// <summary>
        /// Line split characters.
        /// </summary>
        private static readonly char[] newlineChars = Environment.NewLine.ToCharArray();

        /// <summary>
        /// Buffer for the current line.
        /// </summary>
        private readonly StringBuilder currentLineCache = new StringBuilder(250);

        /// <summary>
        /// Cache of the visible text.
        /// </summary>
        private readonly FixedSizeConcurrentQueue<string> visibleTextCache = new FixedSizeConcurrentQueue<string>(DefaultMaxNumberLines);

        /// <summary>
        /// Text box handle that displays text.
        /// </summary>
        private readonly TextBox textBox;

        /// <summary>
        /// Delegate for updating the text box.
        /// </summary>
        private readonly Action updateText;

        public override Encoding Encoding => Encoding.Unicode;

        /// <summary>
        /// Creates a new TextBoxConsole.
        /// </summary>
        public TextBoxWriter(TextBox output)
        {
            textBox = output;

            updateText = new Action(UpdateText);
        }

        /// <summary>
        /// Write text to outputter.
        /// </summary>
        public override void Write(char value)
        {
            base.Write(value);

            if (!newlineChars.Contains(value))
            {
                // Collect characters in line
                currentLineCache.Append(value);
                return;
            }

            // Newline
            // Ignore empty lines
            if (currentLineCache.Length < 1)
                return;

            // Store line in text cache and flush it
            string newText = currentLineCache.ToString();
            currentLineCache.Clear();
            visibleTextCache.Enqueue(newText);
            textBox.Dispatcher.BeginInvoke(updateText);
        }

        private void UpdateText()
        {
            bool doScroll = Math.Abs(textBox.ExtentHeight - (textBox.VerticalOffset + textBox.ViewportHeight)) < (textBox.FontSize * 3);
            textBox.Text = string.Join(Environment.NewLine, visibleTextCache.ToArray());
            if (doScroll)
                textBox.ScrollToEnd();
        }
    }
}
