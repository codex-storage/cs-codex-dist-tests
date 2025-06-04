using Logging;

namespace KubernetesWorkflow
{
    public interface ILogHandler
    {
        void Log(Stream log);
    }

    public abstract class LogHandler : ILogHandler
    {
        public void Log(Stream log)
        {
            using var reader = new StreamReader(log);
            var line = reader.ReadLine();
            while (line != null)
            {
                ProcessLine(line);
                line = reader.ReadLine();
            }
        }

        protected abstract void ProcessLine(string line);
    }

    public class WriteToFileLogHandler : LogHandler, ILogHandler
    {
        public WriteToFileLogHandler(ILog sourceLog, string description, string addFileName)
        {
            LogFile = sourceLog.CreateSubfile(addFileName);

            var msg = $"{description} -->> {LogFile.Filename}";
            sourceLog.Log(msg);

            LogFile.Write(msg);
            LogFile.Write(description);
        }

        public LogFile LogFile { get; }

        protected override void ProcessLine(string line)
        {
            // This line is not useful and has no topic so we can't filter it with
            // normal log-level controls.
            if (line.Contains("Received JSON-RPC response") && !line.Contains("topics=")) return;
            if (line.Contains("object field not marked with serialize, skipping")) return;

            LogFile.Write(line);
        }
    }
}
