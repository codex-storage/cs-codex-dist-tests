using Logging;

namespace TestNetRewarder
{
    public class BufferLogger : ILog
    {
        private readonly List<string> lines = new List<string>();

        public void AddStringReplace(string from, string to)
        {
            throw new NotImplementedException();
        }

        public LogFile CreateSubfile(string ext = "log")
        {
            throw new NotImplementedException();
        }

        public void Debug(string message = "", int skipFrames = 0)
        {
            lines.Add(message);
        }

        public void Error(string message)
        {
            lines.Add($"Error: {message}");
        }

        public void Log(string message)
        {
            lines.Add(message);
        }

        public string[] Get()
        {
            var result = lines.ToArray();
            lines.Clear();
            return result;
        }
    }
}
