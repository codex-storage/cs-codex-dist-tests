using Logging;

namespace DistTestCore.Logs
{
    public abstract class BaseTestLog : ILog
    {
        private readonly ILog backingLog;

        protected BaseTestLog(ILog backingLog, string deployId)
        {
            this.backingLog = backingLog;

            DeployId = deployId;
        }

        public string DeployId { get; }

        public void AddStringReplace(string from, string to)
        {
            backingLog.AddStringReplace(from, to);
        }

        public LogFile CreateSubfile(string addName, string ext = "log")
        {
            return backingLog.CreateSubfile(addName, ext);
        }

        public void Debug(string message = "", int skipFrames = 0)
        {
            backingLog.Debug(message, skipFrames);
        }

        public void Error(string message)
        {
            backingLog.Error(message);
        }

        public string GetFullName()
        {
            return backingLog.GetFullName();
        }

        public void Log(string message)
        {
            backingLog.Log(message);
        }

        public void Raw(string message)
        {
            backingLog.Raw(message);
        }

        public void WriteLogTag()
        {
            var category = NameUtils.GetCategoryName();
            var name = NameUtils.GetTestMethodName();
            backingLog.Raw($"{DeployId} {category} {name}");
        }

        protected static ILog CreateMainLog(string fullName, string name)
        {
            return new LogSplitter(
                new FileLog(fullName),
                new ConsoleLog()
            );
        }
    }
}
