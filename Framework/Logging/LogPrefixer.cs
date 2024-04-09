namespace Logging
{
    public class LogPrefixer : ILog
    {
        private readonly ILog backingLog;

        public LogPrefixer(ILog backingLog)
        {
            this.backingLog = backingLog;
        }

        public LogPrefixer(ILog backingLog, string prefix)
        {
            this.backingLog = backingLog;
            Prefix = prefix;
        }

        public string Prefix { get; set; } = string.Empty;


        public LogFile CreateSubfile(string ext = "log")
        {
            return backingLog.CreateSubfile(ext);
        }

        public void Debug(string message = "", int skipFrames = 0)
        {
            backingLog.Debug(Prefix + message, skipFrames);
        }

        public void Error(string message)
        {
            backingLog.Error(Prefix + message);
        }

        public void Log(string message)
        {
            backingLog.Log(Prefix + message);
        }

        public void AddStringReplace(string from, string to)
        {
            backingLog.AddStringReplace(from, to);
        }
    }
}
