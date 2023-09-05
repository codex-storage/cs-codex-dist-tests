using DistTestCore;
using NUnit.Framework;

namespace Tests.DownloadConnectivityTests
{
    public class FulllyConnectedFetchTests : AutoBootstrapDistTest
    {
        [Test]
        public void MetricsDoesNotInterfereWithFetch()
        {
            SetupCodexNodes(2, s => s.EnableMetrics());

            AssertAllNodesConnected();
        }

        [Test]
        public void MarketplaceDoesNotInterfereWithFetch()
        {
            SetupCodexNodes(2, s => s.EnableMetrics().EnableMarketplace(1000.TestTokens()));

            AssertAllNodesConnected();
        }

        [Test]
        [Combinatorial]
        public void FullyConnectedFetchTest([Values(1, 3, 5)] int numberOfNodes)
        {
            SetupCodexNodes(numberOfNodes);

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            CreatePeerFetchTestHelpers().AssertFullFetchInterconnectivity(GetAllOnlineCodexNodes());
        }
    }
}
