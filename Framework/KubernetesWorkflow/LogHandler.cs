using Logging;

namespace KubernetesWorkflow
{
    public interface ILogHandler
    {
        void Log(Stream log, Func<string?, string?> replacer);
    }

    public abstract class LogHandler : ILogHandler
    {
        public void Log(Stream log, Func<string?, string?> replacer)
        {
            using var reader = new StreamReader(log);
            var line = reader.ReadLine();
            while (line != null)
            {
                line = replacer(reader.ReadLine());
                if (line != null) ProcessLine(line);
            }
        }

        protected abstract void ProcessLine(string line);
    }

    public class WriteToFileLogHandler : LogHandler, ILogHandler
    {
        public WriteToFileLogHandler(ILog sourceLog, string description)
        {
            LogFile = sourceLog.CreateSubfile();

            var msg = $"{description} -->> {LogFile.FullFilename}";
            sourceLog.Log(msg);

            LogFile.Write(msg);
            LogFile.WriteRaw(description);
        }

        public LogFile LogFile { get; }

        protected override void ProcessLine(string line)
        {
            LogFile.WriteRaw(line);
        }
    }
}
