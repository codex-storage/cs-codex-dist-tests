namespace Logging
{
    public class LogSplitter : ILog
    {
        private readonly ILog[] targetLogs;

        public LogSplitter(params ILog[] targetLogs)
        {
            this.targetLogs = targetLogs;
        }

        public void AddStringReplace(string from, string to)
        {
            OnAll(l => l.AddStringReplace(from, to));
        }

        public LogFile CreateSubfile(string ext = "log")
        {
            return targetLogs.First().CreateSubfile(ext);
        }

        public void Debug(string message = "", int skipFrames = 0)
        {
            OnAll(l => l.Debug(message, skipFrames + 2));
        }

        public void Error(string message)
        {
            OnAll(l => l.Error(message));
        }

        public void Log(string message)
        {
            OnAll(l => l.Log(message));
        }

        private void OnAll(Action<ILog> action)
        {
            foreach (var t in targetLogs) action(t);
        }
    }
}
