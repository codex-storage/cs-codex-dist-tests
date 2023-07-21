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
            methodName = NameUtils.GetTestMethodName(name);
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
    }
}
