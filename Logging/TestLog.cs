using NUnit.Framework;
using Utils;

namespace Logging
{
    public class TestLog
    {
        private readonly NumberSource subfileNumberSource = new NumberSource(0);
        private readonly LogFile file;
        private readonly DateTime now;
        private readonly LogConfig config;

        public TestLog(LogConfig config)
        {
            this.config = config;
            now = DateTime.UtcNow;

            var name = GetTestName();
            file = new LogFile(config, now, name);

            Log($"Begin: {name}");
        }

        public void Log(string message)
        {
            file.Write(message);
        }

        public void Error(string message)
        {
            Log($"[ERROR] {message}");
        }

        public void EndTest()
        {
            var result = TestContext.CurrentContext.Result;

            Log($"Finished: {GetTestName()} = {result.Outcome.Status}");
            if (!string.IsNullOrEmpty(result.Message))
            {
                Log(result.Message);
                Log($"{result.StackTrace}");
            }

            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                RenameLogFile();
            }
        }

        private void RenameLogFile()
        {
            file.ConcatToFilename("_FAILED");
        }

        public LogFile CreateSubfile(string ext = "log")
        {
            return new LogFile(config, now, $"{GetTestName()}_{subfileNumberSource.GetNextNumber().ToString().PadLeft(6, '0')}", ext);
        }

        private static string GetTestName()
        {
            var test = TestContext.CurrentContext.Test;
            var className = test.ClassName!.Substring(test.ClassName.LastIndexOf('.') + 1);
            var args = FormatArguments(test);
            return $"{className}.{test.MethodName}{args}";
        }

        private static string FormatArguments(TestContext.TestAdapter test)
        {
            if (test.Arguments == null || !test.Arguments.Any()) return "";
            return $"[{string.Join(',', test.Arguments)}]";
        }
    }
}
