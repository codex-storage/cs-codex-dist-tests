using Logging;
using NUnit.Framework;

namespace DistTestCore.Logs
{
    public interface ICodexNodeLog
    {
        void AssertLogContains(string expectedString);
    }

    public class CodexNodeLog : ICodexNodeLog
    {
        private readonly LogFile logFile;
        private readonly OnlineCodexNode owner;

        public CodexNodeLog(LogFile logFile, OnlineCodexNode owner)
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

            Assert.Fail($"{owner.GetName()} Unable to find string '{expectedString}' in CodexNode log file {logFile.FullFilename}");
        }
    }
}
