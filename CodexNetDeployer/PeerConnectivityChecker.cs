using DistTestCore.Helpers;
using Logging;

namespace CodexNetDeployer
{
    public class PeerConnectivityChecker
    {
        public void CheckConnectivity(List<CodexNodeStartResult> startResults)
        {
            var checker = new PeerConnectionTestHelpers(new ConsoleLogger());

            var access = startResults.Select(r => r.Access);

            checker.AssertFullyConnected(access);
        }
    }

    public class ConsoleLogger : BaseLog
    {
        public ConsoleLogger() : base(false)
        {
        }

        protected override string GetFullName()
        {
            return "CONSOLE";
        }

        public override void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
