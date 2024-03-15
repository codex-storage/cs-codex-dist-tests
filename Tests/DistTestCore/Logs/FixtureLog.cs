using Logging;

namespace DistTestCore.Logs
{
    public class FixtureLog : BaseTestLog
    {
        private readonly string fullName;
        private readonly string deployId;

        public FixtureLog(LogConfig config, DateTime start, string deployId, string name = "") : base(deployId)
        {
            this.deployId = deployId;
            fullName = NameUtils.GetFixtureFullName(config, start, name);
        }

        public TestLog CreateTestLog(string name = "")
        {
            return new TestLog(fullName, deployId, name);
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