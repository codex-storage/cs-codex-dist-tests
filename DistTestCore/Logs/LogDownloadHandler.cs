using KubernetesWorkflow;
using Logging;

namespace DistTestCore.Logs
{
    public class LogDownloadHandler : LogHandler, ILogHandler
    {
        private readonly OnlineCodexNode node;
        private readonly LogFile log;

        public LogDownloadHandler(OnlineCodexNode node, string description, LogFile log)
        {
            this.node = node;
            this.log = log;

            log.Write($"{description} -->> {log.FullFilename}");
            log.WriteRaw(description);
        }

        public CodexNodeLog CreateCodexNodeLog()
        {
            return new CodexNodeLog(log, node);
        }

        protected override void ProcessLine(string line)
        {
            log.WriteRaw(line);
        }
    }
}
