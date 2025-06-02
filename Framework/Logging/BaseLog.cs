using Utils;

namespace Logging
{
    public interface ILog
    {
        void Log(string message);
        void Debug(string message = "", int skipFrames = 0);
        void Error(string message);
        void Raw(string message);
        void AddStringReplace(string from, string to);
        LogFile CreateSubfile(string addName, string ext = "log");
        string GetFullName();
    }

    public abstract class BaseLog : ILog
    {
        public static bool EnableDebugLogging { get; set; } = false;

        private readonly NumberSource subfileNumberSource = new NumberSource(0);
        private readonly List<BaseLogStringReplacement> replacements = new List<BaseLogStringReplacement>();
        private LogFile? logFile;

        public BaseLog()
        {
            IsDebug =
                EnableDebugLogging ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LOGDEBUG")) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DEBUGLOG"));
        }

        protected bool IsDebug { get; private set; }

        public abstract string GetFullName();

        public LogFile LogFile 
        {
            get
            {
                if (logFile == null) logFile = new LogFile(GetFullName() + ".log");
                return logFile;
            }
        }

        public virtual void Log(string message)
        {
            LogFile.Write(ApplyReplacements(message));
        }

        public void Debug(string message = "", int skipFrames = 0)
        {
            if (IsDebug)
            {
                var callerName = DebugStack.GetCallerName(skipFrames);
                Log($"(debug)({callerName}) {message}");
            }
        }

        public virtual void Error(string message)
        {
            var msg = $"[ERROR] {message}";
            Console.WriteLine(msg);
            Log(msg);
        }

        public void Raw(string message)
        {
            LogFile.Write(message);
        }

        public virtual void AddStringReplace(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from)) return;
            if (replacements.Any(r => r.From == from)) return;
            replacements.Add(new BaseLogStringReplacement(from, to));
        }

        public virtual void Delete()
        {
            File.Delete(LogFile.Filename);
        }

        public LogFile CreateSubfile(string addName, string ext = "log")
        {
            addName = addName
                .Replace("<", "")
                .Replace(">", "");

            return new LogFile($"{GetFullName()}_{GetSubfileNumber()}_{addName}.{ext}");
        }

        protected string ApplyReplacements(string str)
        {
            if (IsDebug) return str;
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
