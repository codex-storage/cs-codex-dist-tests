using Logging;

namespace Core
{
    public interface IDownloadedLog
    {
        bool DoesLogContain(string expectedString);
        string[] FindLinesThatContain(params string[] tags);
        void DeleteFile();
    }

    internal class DownloadedLog : IDownloadedLog
    {
        private readonly LogFile logFile;

        internal DownloadedLog(LogFile logFile)
        {
            this.logFile = logFile;
        }

        public bool DoesLogContain(string expectedString)
        {
            using var file = File.OpenRead(logFile.FullFilename);
            using var streamReader = new StreamReader(file);

            var line = streamReader.ReadLine();
            while (line != null)
            {
                if (line.Contains(expectedString)) return true;
                line = streamReader.ReadLine();
            }

            //Assert.Fail($"{owner} Unable to find string '{expectedString}' in CodexNode log file {logFile.FullFilename}");
            return false;
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
