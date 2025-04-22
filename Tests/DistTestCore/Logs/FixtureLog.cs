using Logging;

namespace DistTestCore.Logs
{
    public class FixtureLog : BaseTestLog
    {
        private readonly ILog backingLog;
        private readonly string deployId;

        public FixtureLog(ILog backingLog, string deployId)
            : base(backingLog, deployId)
        {
            this.backingLog = backingLog;
            this.deployId = deployId;
        }

        public TestLog CreateTestLog(string name = "")
        {
            return TestLog.Create(this, name);
        }

        public static FixtureLog Create(LogConfig config, DateTime start, string deployId, string name = "")
        {
            var fullName = NameUtils.GetFixtureFullName(config, start, name);
            var log = CreateMainLog(fullName, name);
            return new FixtureLog(log, deployId);
        }
    }
}
