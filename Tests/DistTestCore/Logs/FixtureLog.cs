using Logging;

namespace DistTestCore.Logs
{
    public class FixtureLog : BaseTestLog
    {
        private readonly string fullName;

        public FixtureLog(LogConfig config, DateTime start, string name = "")
        {
            fullName = NameUtils.GetFixtureFullName(config, start, name);
        }

        public TestLog CreateTestLog(string name = "")
        {
            return new TestLog(fullName, name);
        }

        public void DeleteFolder()
        {
            Directory.Delete(fullName, true);
        }

        protected override string GetFullName()
        {
            return fullName;
        }
    }
}
