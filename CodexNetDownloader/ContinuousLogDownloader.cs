using DistTestCore;
using KubernetesWorkflow;

namespace CodexNetDownloader
{
    public class ContinuousLogDownloader
    {
        private readonly TestLifecycle lifecycle;
        private readonly Configuration config;

        public ContinuousLogDownloader(TestLifecycle lifecycle, Configuration config)
        {
            this.lifecycle = lifecycle;
            this.config = config;
        }

        public void Run()
        {
            while (true)
            {
                UpdateLogs();

                Thread.Sleep(TimeSpan.FromSeconds(30));
            }
        }

        private void UpdateLogs()
        {
            Console.WriteLine("Updating logs...");
            foreach (var container in config.CodexDeployment.CodexContainers)
            {
                UpdateLog(container);
            }
        }

        private void UpdateLog(RunningContainer container)
        {
            var filepath = Path.Combine(config.OutputPath, GetLogName(container));
            if (!File.Exists(filepath)) File.WriteAllLines(filepath, new[] { "" });

            var appender = new LogAppender(filepath);

            lifecycle.CodexStarter.DownloadLog(container, appender);
        }

        private static string GetLogName(RunningContainer container)
        {
            return container.Name
                .Replace("<","")
                .Replace(">", "")
                + ".log";
        }
    }

    public class LogAppender : ILogHandler
    {
        private readonly string filename;

        public LogAppender(string filename)
        {
            this.filename = filename;
        }

        public void Log(Stream log)
        {
            using var reader = new StreamReader(log);
            var currentLines = File.ReadAllLines(filename);
            var line = reader.ReadLine();
            while (line != null)
            {
                AppendLineIfNew(line, currentLines);
                line = reader.ReadLine();
            }
        }

        private void AppendLineIfNew(string line, string[] currentLines)
        {
            if (!currentLines.Contains(line))
            {
                File.AppendAllLines(filename, new[] { line });
            }
        }
    }
}
