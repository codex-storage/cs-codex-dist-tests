using Utils;

namespace Logging
{
    public interface ILog
    {
        void Log(string message);
        void Debug(string message = "", int skipFrames = 0);
        void Error(string message);
        void AddStringReplace(string from, string to);
        LogFile CreateSubfile(string ext = "log");
    }

    public abstract class BaseLog : ILog
    {
        private readonly NumberSource subfileNumberSource = new NumberSource(0);
        private readonly bool debug;
        private readonly List<BaseLogStringReplacement> replacements = new List<BaseLogStringReplacement>();
        private LogFile? logFile;

        protected BaseLog(bool debug)
        {
            this.debug = debug;
        }

        protected abstract string GetFullName();

        public LogFile LogFile 
        {
            get
            {
                if (logFile == null) logFile = new LogFile(GetFullName(), "log");
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

        public virtual void AddStringReplace(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from)) return;
            if (replacements.Any(r => r.From == from)) return;
            replacements.Add(new BaseLogStringReplacement(from, to));
        }

        public virtual void Delete()
        {
            File.Delete(LogFile.FullFilename);
        }

        public LogFile CreateSubfile(string ext = "log")
        {
            return new LogFile($"{GetFullName()}_{GetSubfileNumber()}", ext);
        }

        private string ApplyReplacements(string str)
        {
            foreach (var replacement in replacements)
            {
                str = replacement.Apply(str);
            }
            return str;
        }

        private string GetSubfileNumber()
        {
            return subfileNumberSource.GetNextNumber().ToString().PadLeft(6, '0');
        }
    }

    public class BaseLogStringReplacement
    {
        public BaseLogStringReplacement(string from, string to)
        {
            From = from;
            To = to;

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || from == to) throw new ArgumentException();
        }

        public string From { get; }
        public string To { get; }

        public string Apply(string msg)
        {
            return msg.Replace(From, To);
        }
    }
}
