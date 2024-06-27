using KubernetesWorkflow;
using Logging;

namespace Core
{
    public interface IDownloadedLog
    {
        void IterateLines(Action<string> action);
        string[] GetLinesContaining(string expectedString);
        string[] FindLinesThatContain(params string[] tags);
        void DeleteFile();
    }

    internal class DownloadedLog : IDownloadedLog
    {
        private readonly LogFile logFile;

        internal DownloadedLog(WriteToFileLogHandler logHandler)
        {
            logFile = logHandler.LogFile;
        }

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

        public void DeleteFile()
        {
            File.Delete(logFile.FullFilename);
        }
    }
}
