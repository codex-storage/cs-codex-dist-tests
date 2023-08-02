using DistTestCore;
using DistTestCore.Codex;
using KubernetesWorkflow;

namespace ContinuousTests
{
    public class ContinuousLogDownloader
    {
        private readonly TestLifecycle lifecycle;
        private readonly RunningContainer[] containers;
        //private readonly CodexDeployment deployment;
        private readonly string outputPath;
        private readonly CancellationToken cancelToken;

        public ContinuousLogDownloader(TestLifecycle lifecycle, RunningContainer[] containers, string outputPath, CancellationToken cancelToken)
        {
            this.lifecycle = lifecycle;
            this.containers = containers;
            //this.deployment = deployment;
            this.outputPath = outputPath;
            this.cancelToken = cancelToken;
        }

        public void Run()
        {
            while (!cancelToken.IsCancellationRequested)
            {
                UpdateLogs();

                cancelToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
            }

            // After testing has stopped, we wait a little bit and fetch the logs one more time.
            // If our latest fetch was not recent, interesting test-related log activity might
            // not have been captured yet.
            Thread.Sleep(TimeSpan.FromSeconds(10));
            UpdateLogs();
        }

        private void UpdateLogs()
        {
            foreach (var container in containers)
            {
                UpdateLog(container);
            }
        }

        private void UpdateLog(RunningContainer container)
        {
            var filepath = Path.Combine(outputPath, GetLogName(container));
            if (!File.Exists(filepath))
            {
                File.WriteAllLines(filepath, new[] { container.Name });
            }

            var appender = new LogAppender(filepath);

            lifecycle.CodexStarter.DownloadLog(container, appender);
        }

        private static string GetLogName(RunningContainer container)
        {
            return container.Name
                .Replace("<", "")
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
            var lines = File.ReadAllLines(filename);
            var lastLine = lines.Last();
            var recording = lines.Length < 3;
            var line = reader.ReadLine();
            while (line != null)
            {
                if (recording)
                {
                    File.AppendAllLines(filename, new[] { line });
                }
                else
                {
                    recording = line == lastLine;
                }

                line = reader.ReadLine();
            }
        }
    }
}
