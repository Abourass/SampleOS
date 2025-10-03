using UnityEngine;
using System.Text;

namespace SampleOS.Core.Terminal
{
    /// <summary>
    /// Represents an output stream (stdout or stderr)
    /// </summary>
    public interface ITerminalStream
    {
        void Write(string text);
        void WriteLine(string text);
        void SetColor(Color color);
    }

    /// <summary>
    /// Provides access to both output streams
    /// </summary>
    public class TerminalStreams
    {
        public ITerminalStream Stdout { get; }
        public ITerminalStream Stderr { get; }

        public TerminalStreams(ITerminalStream stdout, ITerminalStream stderr)
        {
            Stdout = stdout;
            Stderr = stderr;
        }
    }

    /// <summary>
    /// Direct terminal stream - writes to ITerminalOutput
    /// </summary>
    public class DirectTerminalStream : ITerminalStream
    {
        private readonly ITerminalOutput output;
        private readonly Color defaultColor;

        public DirectTerminalStream(ITerminalOutput output, Color defaultColor)
        {
            this.output = output;
            this.defaultColor = defaultColor;
        }

        public void Write(string text)
        {
            output.SetColor(defaultColor);
            output.AppendText(text);
        }

        public void WriteLine(string text)
        {
            Write(text + "\n");
        }

        public void SetColor(Color color)
        {
            output.SetColor(color);
        }
    }

    /// <summary>
    /// Buffered stream - captures output for piping
    /// </summary>
    public class BufferedStream : ITerminalStream
    {
        private readonly StringBuilder buffer = new StringBuilder();
        private Color currentColor = Color.white;

        public string Content => buffer.ToString();
        public bool HasContent => buffer.Length > 0;

        public void Write(string text)
        {
            buffer.Append(text);
        }

        public void WriteLine(string text)
        {
            buffer.AppendLine(text);
        }

        public void SetColor(Color color)
        {
            currentColor = color;
        }

        public void Clear()
        {
            buffer.Clear();
        }
    }
}
