using KubernetesWorkflow;
using Logging;

namespace Core
{
    internal class LogDownloadHandler : LogHandler, ILogHandler
    {
        private readonly LogFile log;

        internal LogDownloadHandler(string description, LogFile log)
        {
            this.log = log;

            log.Write($"{description} -->> {log.FullFilename}");
            log.WriteRaw(description);
        }

        internal IDownloadedLog DownloadLog()
        {
            return new DownloadedLog(log);
        }

        protected override void ProcessLine(string line)
        {
            log.WriteRaw(line);
        }
    }
}
