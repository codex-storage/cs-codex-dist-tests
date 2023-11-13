using CodexTests.Helpers;
using Logging;

namespace CodexNetDeployer
{
    public class PeerConnectivityChecker
    {
        public void CheckConnectivity(List<CodexNodeStartResult> startResults)
        {
            var log = new ConsoleLog();
            var checker = new PeerConnectionTestHelpers(log);
            var nodes = startResults.Select(r => r.CodexNode);

            checker.AssertFullyConnected(nodes);
        }
    }
}
