using Logging;
using NUnit.Framework;

namespace DistTestCore.Logs
{
    public interface IDownloadedLog
    {
        void AssertLogContains(string expectedString);
        string[] FindLinesThatContain(params string[] tags);
        void DeleteFile();
    }

    public class DownloadedLog : IDownloadedLog
    {
        private readonly LogFile logFile;
        private readonly string owner;

        public DownloadedLog(LogFile logFile, string owner)
        {
            this.logFile = logFile;
            this.owner = owner;
        }

        public void AssertLogContains(string expectedString)
        {
            using var file = File.OpenRead(logFile.FullFilename);
            using var streamReader = new StreamReader(file);

            var line = streamReader.ReadLine();
            while (line != null)
            {
                if (line.Contains(expectedString)) return;
                line = streamReader.ReadLine();
            }

            Assert.Fail($"{owner} Unable to find string '{expectedString}' in CodexNode log file {logFile.FullFilename}");
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
