using Logging;
using NUnit.Framework;

namespace DistTestCore.Logs
{
    public interface IDownloadedLog
    {
        void AssertLogContains(string expectedString);
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
    }
}
