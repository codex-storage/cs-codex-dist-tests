using NUnit.Framework;

namespace CodexDistTestCore
{
    public interface ICodexNodeLog
    {
        void AssertLogContains(string expectedString);
    }

    public class CodexNodeLog : ICodexNodeLog
    {
        private readonly LogFile logFile;

        public CodexNodeLog(LogFile logFile)
        {
            this.logFile = logFile;
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

            Assert.Fail($"Unable to find string '{expectedString}' in CodexNode log file {logFile.FilenameWithoutPath}");
        }
    }
}
