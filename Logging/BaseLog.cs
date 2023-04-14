namespace Logging
{
    public abstract class BaseLog
    {
        private bool hasFailed;
        private LogFile? logFile;
        
        protected abstract LogFile CreateLogFile();

        protected LogFile LogFile 
        {
            get
            {
                if (logFile == null) logFile = CreateLogFile();
                return logFile;
            }
        }

        public void Log(string message)
        {
            LogFile.Write(message);
        }

        public void Error(string message)
        {
            Log($"[ERROR] {message}");
        }

        public void MarkAsFailed()
        {
            if (hasFailed) return;
            hasFailed = true;
            LogFile.ConcatToFilename("_FAILED");
        }
    }
}
