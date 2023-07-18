using KubernetesWorkflow;
using Logging;

namespace DistTestCore.Logs
{
    public class LogDownloadHandler : LogHandler, ILogHandler
    {
        private readonly RunningContainer container;
        private readonly LogFile log;

        public LogDownloadHandler(RunningContainer container, string description, LogFile log)
        {
            this.container = container;
            this.log = log;

            log.Write($"{description} -->> {log.FullFilename}");
            log.WriteRaw(description);
        }

        public DownloadedLog DownloadLog()
        {
            return new DownloadedLog(log, container.Name);
        }

        protected override void ProcessLine(string line)
        {
            log.WriteRaw(line);
        }
    }
}
