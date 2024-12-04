using Logging;

namespace KubernetesWorkflow
{
    public interface IDownloadedLog
    {
        string ContainerName { get; }

        void IterateLines(Action<string> action);
        void IterateLines(Action<string> action, params string[] thatContain);
        string[] GetLinesContaining(string expectedString);
        string[] FindLinesThatContain(params string[] tags);
        string GetFilepath();
        void DeleteFile();
    }

    internal class DownloadedLog : IDownloadedLog
    {
        private readonly LogFile logFile;

        internal DownloadedLog(WriteToFileLogHandler logHandler, string containerName)
        {
            logFile = logHandler.LogFile;
            ContainerName = containerName;
        }

        public string ContainerName { get; }

        public void IterateLines(Action<string> action)
        {
            using var file = File.OpenRead(logFile.FullFilename);
            using var streamReader = new StreamReader(file);

            var line = streamReader.ReadLine();
            while (line != null)
            {
                action(line);
                line = streamReader.ReadLine();
            }
        }

        public void IterateLines(Action<string> action, params string[] thatContain)
        {
            IterateLines(line =>
            {
                if (thatContain.All(line.Contains))
                {
                    action(line);
                }
            });
        }

        public string[] GetLinesContaining(string expectedString)
        {
            return FindLinesThatContain([expectedString]);
        }

        public string[] FindLinesThatContain(params string[] tags)
        {
            var result = new List<string>();
            IterateLines(result.Add, tags);
            return result.ToArray();
        }

        public string GetFilepath()
        {
            return logFile.FullFilename;
        }

        public void DeleteFile()
        {
            File.Delete(logFile.FullFilename);
        }
    }
}
