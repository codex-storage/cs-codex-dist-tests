using KubernetesWorkflow;
using Logging;

namespace Core
{
    public interface IDownloadedLog
    {
        string ContainerName { get; }

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

        public void IterateLines(Action<string> action, params string[] thatContain)
        {
            using var file = File.OpenRead(logFile.FullFilename);
            using var streamReader = new StreamReader(file);

            var line = streamReader.ReadLine();
            while (line != null)
            {
                if (thatContain.All(line.Contains))
                {
                    action(line);
                }
                line = streamReader.ReadLine();
            }
        }

        public string[] GetLinesContaining(string expectedString)
        {
            using var file = File.OpenRead(logFile.FullFilename);
            using var streamReader = new StreamReader(file);
            var lines = new List<string>();

            var line = streamReader.ReadLine();
            while (line != null)
            {
                if (line.Contains(expectedString))
                {
                    lines.Add(line);
                }
                line = streamReader.ReadLine();
            }

            return lines.ToArray(); ;
        }

        public string[] FindLinesThatContain(params string[] tags)
        {
            var result = new List<string>();
            using var file = File.OpenRead(logFile.FullFilename);
            using var streamReader = new StreamReader(file);

            var line = streamReader.ReadLine();
            while (line != null)
            {
                if (tags.All(line.Contains))
                {
                    result.Add(line);
                }

                line = streamReader.ReadLine();
            }

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
