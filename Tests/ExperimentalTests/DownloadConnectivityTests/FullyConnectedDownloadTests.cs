using CodexClient;
using CodexContractsPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.DownloadConnectivityTests
{
    [TestFixture]
    public class FullyConnectedDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        public void MetricsDoesNotInterfereWithPeerDownload()
        {
            var nodes = StartCodex(2, s => s.EnableMetrics());

            AssertAllNodesConnected(nodes);
        }

        [Test]
        public void MarketplaceDoesNotInterfereWithPeerDownload()
        {
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth);
            var nodes = StartCodex(2, s => s.EnableMarketplace(geth, contracts, m => m
                .WithInitial(10.Eth(), 1000.TstWei())));

            AssertAllNodesConnected(nodes);
        }

        [Test]
        [Combinatorial]
        public void FullyConnectedDownloadTest(
            [Values(2, 5)] int numberOfNodes,
            [Values(1, 10)] int sizeMBs)
        {
            var nodes = StartCodex(numberOfNodes);

            AssertAllNodesConnected(nodes, sizeMBs);
        }

        private void AssertAllNodesConnected(IEnumerable<ICodexNode> nodes, int sizeMBs = 10)
        {
            CreatePeerDownloadTestHelpers().AssertFullDownloadInterconnectivity(nodes, sizeMBs.MB());
        }
    }
}
