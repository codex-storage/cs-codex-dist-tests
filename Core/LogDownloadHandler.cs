using KubernetesWorkflow;
using Logging;

namespace Core
{
    public class LogDownloadHandler : LogHandler, ILogHandler
    {
        private readonly LogFile log;

        public LogDownloadHandler(string description, LogFile log)
        {
            this.log = log;

            log.Write($"{description} -->> {log.FullFilename}");
            log.WriteRaw(description);
        }

        public IDownloadedLog DownloadLog()
        {
            return new DownloadedLog(log);
        }

        protected override void ProcessLine(string line)
        {
            log.WriteRaw(line);
        }
    }
}
