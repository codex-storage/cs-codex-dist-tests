using Logging;

namespace DistTestCore.Logs
{
    public class FixtureLog : BaseTestLog
    {
        public FixtureLog(ILog backingLog, string deployId)
            : base(backingLog, deployId)
        {
        }

        public TestLog CreateTestLog(string name = "")
        {
            var result = TestLog.Create(this, name);
            result.Log(NameUtils.GetRawFixtureName());
            return result;
        }

        public static FixtureLog Create(LogConfig config, DateTime start, string deployId, string name = "")
        {
            var fullName = NameUtils.GetFixtureFullName(config, start, name);
            var log = CreateMainLog(fullName, name);
            var result = new FixtureLog(log, deployId);
            result.Log(NameUtils.GetRawFixtureName());
            return result;
        }
    }
}
