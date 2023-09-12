namespace Logging
{
    public class TestLog : BaseLog
    {
        private readonly string methodName;
        private readonly string fullName;
        private bool hasFailed;

        public TestLog(string folder, bool debug, string name = "")
            : base(debug)
        {
            methodName = NameUtils.GetTestMethodName(name);
            fullName = Path.Combine(folder, methodName);

            Log($"*** Begin: {methodName}");
        }

        public void MarkAsFailed()
        {
            if (hasFailed) return;
            hasFailed = true;
            LogFile.ConcatToFilename("_FAILED");
        }

        protected override string GetFullName()
        {
            return fullName;
        }
    }
}
