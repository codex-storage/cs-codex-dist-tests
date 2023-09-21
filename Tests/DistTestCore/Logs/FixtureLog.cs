using Logging;

namespace DistTestCore.Logs
{
    public class FixtureLog : BaseTestLog
    {
        private readonly string fullName;
        private readonly LogConfig config;

        public FixtureLog(LogConfig config, DateTime start, string name = "")
            : base(config.DebugEnabled)
        {
            fullName = NameUtils.GetFixtureFullName(config, start, name);
            this.config = config;
        }

        public TestLog CreateTestLog(string name = "")
        {
            return new TestLog(fullName, config.DebugEnabled, name);
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
