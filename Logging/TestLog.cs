using NUnit.Framework;
using Utils;

namespace Logging
{
    public class TestLog : BaseLog
    {
        private readonly NumberSource subfileNumberSource = new NumberSource(0);
        private readonly string methodName;
        private readonly string fullName;

        public TestLog(string folder)
        {
            methodName = GetMethodName();
            fullName = Path.Combine(folder, methodName);

            Log($"*** Begin: {methodName}");
        }

        public LogFile CreateSubfile(string ext = "log")
        {
            return new LogFile($"{fullName}_{GetSubfileNumber()}", ext);
        }

        public void EndTest()
        {
            var result = TestContext.CurrentContext.Result;

            Log($"*** Finished: {methodName} = {result.Outcome.Status}");
            if (!string.IsNullOrEmpty(result.Message))
            {
                Log(result.Message);
                Log($"{result.StackTrace}");
            }

            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                MarkAsFailed();
            }
        }
        protected override LogFile CreateLogFile()
        {
            return new LogFile(fullName, "log");
        }

        private string GetMethodName()
        {
            var test = TestContext.CurrentContext.Test;
            var args = FormatArguments(test);
            return $"{test.MethodName}{args}";
        }

        private string GetSubfileNumber()
        {
            return subfileNumberSource.GetNextNumber().ToString().PadLeft(6, '0');
        }

        private static string FormatArguments(TestContext.TestAdapter test)
        {
            if (test.Arguments == null || !test.Arguments.Any()) return "";
            return $"[{string.Join(',', test.Arguments)}]";
        }
    }
}
