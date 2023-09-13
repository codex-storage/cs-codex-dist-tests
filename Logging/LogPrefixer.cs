namespace Logging
{
    public class LogPrefixer : ILog
    {
        private readonly ILog backingLog;
        private readonly string prefix;

        public LogPrefixer(ILog backingLog, string prefix)
        {
            this.backingLog = backingLog;
            this.prefix = prefix;
        }

        public LogFile CreateSubfile(string ext = "log")
        {
            return backingLog.CreateSubfile(ext);
        }

        public void Debug(string message = "", int skipFrames = 0)
        {
            backingLog.Debug(prefix + message, skipFrames);
        }

        public void Error(string message)
        {
            backingLog.Error(prefix + message);
        }

        public void Log(string message)
        {
            backingLog.Log(prefix + message);
        }
    }
}
