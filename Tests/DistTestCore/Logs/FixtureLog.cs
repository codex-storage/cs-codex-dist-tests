using Logging;

namespace DistTestCore.Logs
{
    public class FixtureLog : BaseTestLog
    {
        public FixtureLog(ILog backingLog, string deployId)
            : base(backingLog, deployId)
        {
        }

        public TestLog CreateTestLog(DateTime start, string name = "")
        {
            return TestLog.Create(this, start, name);
        }

        public static FixtureLog Create(LogConfig config, DateTime start, string deployId, string name = "")
        {
            var fullName = NameUtils.GetFixtureFullName(config, start, name);
            var log = CreateMainLog(fullName, name);
            return new FixtureLog(log, deployId);
        }
    }
}
