using NUnit.Framework;

namespace CodexTests.PeerDiscoveryTests
{
    [TestFixture]
    public class LayeredDiscoveryTests : CodexDistTest
    {
        [Test]
        public void TwoLayersTest()
        {
            var root = StartCodex();
            var l1Source = StartCodex(s => s.WithBootstrapNode(root));
            var l1Node = StartCodex(s => s.WithBootstrapNode(root));
            var l2Target = StartCodex(s => s.WithBootstrapNode(l1Node));

            AssertAllNodesConnected();
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = StartCodex();
            var l1Source = StartCodex(s => s.WithBootstrapNode(root));
            var l1Node = StartCodex(s => s.WithBootstrapNode(root));
            var l2Node = StartCodex(s => s.WithBootstrapNode(l1Node));
            var l3Target = StartCodex(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected();
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        public void NodeChainTest(int chainLength)
        {
            var node = StartCodex();
            for (var i = 1; i < chainLength; i++)
            {
                node = StartCodex(s => s.WithBootstrapNode(node));
            }

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            CreatePeerConnectionTestHelpers().AssertFullyConnected(GetAllOnlineCodexNodes());
        }
    }
}
