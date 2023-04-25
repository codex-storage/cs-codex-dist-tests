using System.Diagnostics;
using Utils;

namespace Logging
{
    public abstract class BaseLog
    {
        private bool hasFailed;
        private LogFile? logFile;
        private readonly bool debug;

        protected BaseLog(bool debug)
        {
            this.debug = debug;
        }

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

        public void Debug(string message = "", int skipFrames = 0)
        {
            if (debug)
            {
                var callerName = DebugStack.GetCallerName(skipFrames);
                Log($"(debug)({callerName}) {message}");
            }
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
