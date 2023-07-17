using NUnit.Framework;

namespace Logging
{
    public class TestLog : BaseLog
    {
        private readonly string methodName;
        private readonly string fullName;

        public TestLog(string folder, bool debug, string name = "")
            : base(debug)
        {
            methodName = GetMethodName(name);
            fullName = Path.Combine(folder, methodName);

            Log($"*** Begin: {methodName}");
        }

        public override void EndTest()
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

        protected override string GetFullName()
        {
            return fullName;
        }

        private string GetMethodName(string name)
        {
            if (!string.IsNullOrEmpty(name)) return name;
            var test = TestContext.CurrentContext.Test;
            var args = FormatArguments(test);
            return ReplaceInvalidCharacters($"{test.MethodName}{args}");
        }

        private static string FormatArguments(TestContext.TestAdapter test)
        {
            if (test.Arguments == null || !test.Arguments.Any()) return "";
            return $"[{string.Join(',', test.Arguments)}]";
        }

        private static string ReplaceInvalidCharacters(string name)
        {
            return name.Replace(":", "_");
        }
    }
}
