using KubernetesWorkflow;
using Logging;

namespace DistTestCore.CodexLogsAndMetrics
{
    public class LogDownloadHandler : ILogHandler
    {
        private readonly string description;
        private readonly LogFile log;

        public LogDownloadHandler(string description, LogFile log)
        {
            this.description = description;
            this.log = log;
        }

        public CodexNodeLog CreateCodexNodeLog()
        {
            return new CodexNodeLog(log);
        }

        public void Log(Stream stream)
        {
            log.Write($"{description} -->> {log.FilenameWithoutPath}");
            log.WriteRaw(description);
            var reader = new StreamReader(stream);
            var line = reader.ReadLine();
            while (line != null)
            {
                log.WriteRaw(line);
                line = reader.ReadLine();
            }
        }
    }
}
