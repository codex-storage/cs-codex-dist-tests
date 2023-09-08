using DistTestCore;
using NUnit.Framework;
using Utils;

namespace Tests.DownloadConnectivityTests
{
    [TestFixture]
    public class FullyConnectedDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        public void MetricsDoesNotInterfereWithPeerDownload()
        {
            SetupCodexNodes(2, s => s.EnableMetrics());

            AssertAllNodesConnected();
        }

        [Test]
        public void MarketplaceDoesNotInterfereWithPeerDownload()
        {
            SetupCodexNodes(2, s => s.EnableMetrics().EnableMarketplace(1000.TestTokens()));

            AssertAllNodesConnected();
        }

        [Test]
        [Combinatorial]
        public void FullyConnectedDownloadTest(
            [Values(1, 3, 5)] int numberOfNodes,
            [Values(1, 10)] int sizeMBs)
        {
            SetupCodexNodes(numberOfNodes);

            AssertAllNodesConnected(sizeMBs);
        }

        private void AssertAllNodesConnected(int sizeMBs = 10)
        {
            CreatePeerDownloadTestHelpers().AssertFullDownloadInterconnectivity(GetAllOnlineCodexNodes(), sizeMBs.MB());
        }
    }
}
