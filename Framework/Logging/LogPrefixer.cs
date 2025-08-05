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

        public LogFile CreateSubfile(string addName, string ext = "log")
        {
            return backingLog.CreateSubfile(addName, ext);
        }

        public void Debug(string message = "", int skipFrames = 0)
        {
            backingLog.Debug(GetPrefix() + message, skipFrames + 1);
        }

        public void Error(string message)
        {
            backingLog.Error(GetPrefix() + message);
        }

        public void Log(string message)
        {
            backingLog.Log(GetPrefix() + message);
        }

        public void AddStringReplace(string from, string to)
        {
            backingLog.AddStringReplace(from, to);
        }

        public void Raw(string message)
        {
            backingLog.Raw(message);
        }

        public string GetFullName()
        {
            return backingLog.GetFullName();
        }

        protected virtual string GetPrefix()
        {
            return Prefix;
        }
    }
}
