namespace Logging
{
    public interface IDownloadedLog
    {
        string SourceName { get; }

        void IterateLines(Action<string> action);
        void IterateLines(Action<string> action, params string[] thatContain);
        string[] GetLinesContaining(string expectedString);
        string[] FindLinesThatContain(params string[] tags);
        string GetFilepath();
        void DeleteFile();
    }

    public class DownloadedLog : IDownloadedLog
    {
        private readonly LogFile logFile;

        public DownloadedLog(string filepath, string sourceName)
        {
            logFile = new LogFile(filepath);
            SourceName = sourceName;
        }

        public DownloadedLog(LogFile logFile, string sourceName)
        {
            this.logFile = logFile;
            SourceName = sourceName;
        }

        public string SourceName { get; }

        public void IterateLines(Action<string> action)
        {
            using var file = File.OpenRead(logFile.Filename);
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
            return logFile.Filename;
        }

        public void DeleteFile()
        {
            File.Delete(logFile.Filename);
        }
    }
}
