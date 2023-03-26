namespace CodexDistTestCore
{
    public interface IPodLogHandler
    {
        void Log(Stream log);
    }

    public class PodLogDownloader
    {
        private readonly TestLog log;
        private readonly IK8sManager k8SManager;

        public PodLogDownloader(TestLog log, IK8sManager k8sManager)
        {
            this.log = log;
            k8SManager = k8sManager;
        }

        public CodexNodeLog DownloadLog(OnlineCodexNode node)
        {
            var description = node.Describe();
            var subFile = log.CreateSubfile();

            log.Log($"Downloading logs for {description} to file {subFile.FilenameWithoutPath}");
            var handler = new PodLogDownloadHandler(description, subFile);
            k8SManager.FetchPodLog(node, handler);
            return handler.CreateCodexNodeLog();
        }
    }

    public class PodLogDownloadHandler : IPodLogHandler
    {
        private readonly string description;
        private readonly LogFile log;

        public PodLogDownloadHandler(string description, LogFile log)
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
