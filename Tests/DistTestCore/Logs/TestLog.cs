using Logging;
using System.Xml.Linq;

namespace DistTestCore.Logs
{
    public class TestLog : BaseTestLog
    {
        public TestLog(ILog backingLog, string methodName, string deployId, string name = "")
            : base(backingLog, deployId)
        {
            backingLog.Log($"*** Begin: {methodName}");
        }

        public static TestLog Create(FixtureLog parentLog, string name = "")
        {
            var methodName = NameUtils.GetTestMethodName(name);
            var fullName = Path.Combine(parentLog.GetFullName(), methodName);
            var backingLog = CreateMainLog(fullName, name);
            return new TestLog(backingLog, methodName, parentLog.DeployId);
        }
    }
}
