using Utils;

namespace Logging
{
    public abstract class BaseLog
    {
        private readonly bool debug;
        private readonly List<BaseLogStringReplacement> replacements = new List<BaseLogStringReplacement>();
        private bool hasFailed;
        private LogFile? logFile;

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

        public virtual void Log(string message)
        {
            LogFile.Write(ApplyReplacements(message));
        }

        public virtual void Debug(string message = "", int skipFrames = 0)
        {
            if (debug)
            {
                var callerName = DebugStack.GetCallerName(skipFrames);
                // We don't use Log because in the debug output we should not have any replacements.
                LogFile.Write($"(debug)({callerName}) {message}");
            }
        }

        public virtual void Error(string message)
        {
            Log($"[ERROR] {message}");
        }

        public virtual void MarkAsFailed()
        {
            if (hasFailed) return;
            hasFailed = true;
            LogFile.ConcatToFilename("_FAILED");
        }

        public virtual void AddStringReplace(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from)) return;
            replacements.Add(new BaseLogStringReplacement(from, to));
        }

        public virtual void Delete()
        {
            File.Delete(LogFile.FullFilename);
        }

        private string ApplyReplacements(string str)
        {
            foreach (var replacement in replacements)
            {
                str = replacement.Apply(str);
            }
            return str;
        }
    }

    public class BaseLogStringReplacement
    {
        private readonly string from;
        private readonly string to;

        public BaseLogStringReplacement(string from, string to)
        {
            this.from = from;
            this.to = to;

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || from == to) throw new ArgumentException();
        }

        public string Apply(string msg)
        {
            return msg.Replace(from, to);
        }
    }
}
