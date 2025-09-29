namespace SampleOS.Core.Terminal
{
    public interface IOutputStream
    {
        void Write(string message);
        void WriteLine(string message);
        void WriteError(string message);
        void WriteErrorLine(string message);
        void Clear();
    }

    public enum OutputStreamType
    {
        StandardOut,
        StandardError
    }

    public class StreamMessage
    {
        public OutputStreamType Stream { get; set; }
        public string Content { get; set; }
        public System.DateTime Timestamp { get; set; }
    }
}
