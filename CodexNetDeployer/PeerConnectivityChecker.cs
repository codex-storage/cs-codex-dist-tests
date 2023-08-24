using DistTestCore.Helpers;
using Logging;

namespace CodexNetDeployer
{
    public class PeerConnectivityChecker
    {
        public void CheckConnectivity(List<CodexNodeStartResult> startResults)
        {
            var log = new ConsoleLog();
            var checker = new PeerConnectionTestHelpers(log);
            var access = startResults.Select(r => r.Access);

            checker.AssertFullyConnected(access);
        }
    }

    public class ConsoleLog : BaseLog
    {
        public ConsoleLog() : base(false)
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
