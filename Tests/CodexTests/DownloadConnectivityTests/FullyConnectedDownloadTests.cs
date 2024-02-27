using CodexContractsPlugin;
using GethPlugin;
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
            AddCodex(2, s => s.EnableMetrics());

            AssertAllNodesConnected();
        }

        [Test]
        public void MarketplaceDoesNotInterfereWithPeerDownload()
        {
            var geth = Ci.StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth);
            AddCodex(2, s => s.EnableMarketplace(geth, contracts, 10.Eth(), 1000.TestTokens()));

            AssertAllNodesConnected();
        }

        [Test]
        [Combinatorial]
        public void FullyConnectedDownloadTest(
            [Values(2, 5)] int numberOfNodes,
            [Values(1, 10)] int sizeMBs)
        {
            AddCodex(numberOfNodes);

            AssertAllNodesConnected(sizeMBs);
        }

        private void AssertAllNodesConnected(int sizeMBs = 10)
        {
            CreatePeerDownloadTestHelpers().AssertFullDownloadInterconnectivity(GetAllOnlineCodexNodes(), sizeMBs.MB());
        }
    }
}
