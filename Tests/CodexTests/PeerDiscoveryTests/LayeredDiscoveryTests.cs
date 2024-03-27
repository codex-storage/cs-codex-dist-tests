using NUnit.Framework;

namespace CodexTests.PeerDiscoveryTests
{
    [TestFixture]
    public class LayeredDiscoveryTests : CodexDistTest
    {
        [Test]
        public void TwoLayersTest()
        {
            var root = AddCodex();
            var l1Source = AddCodex(s => s.WithBootstrapNode(root));
            var l1Node = AddCodex(s => s.WithBootstrapNode(root));
            var l2Target = AddCodex(s => s.WithBootstrapNode(l1Node));

            AssertAllNodesConnected();
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = AddCodex();
            var l1Source = AddCodex(s => s.WithBootstrapNode(root));
            var l1Node = AddCodex(s => s.WithBootstrapNode(root));
            var l2Node = AddCodex(s => s.WithBootstrapNode(l1Node));
            var l3Target = AddCodex(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected();
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        public void NodeChainTest(int chainLength)
        {
            var node = AddCodex();
            for (var i = 1; i < chainLength; i++)
            {
                node = AddCodex(s => s.WithBootstrapNode(node));
            }

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            CreatePeerConnectionTestHelpers().AssertFullyConnected(GetAllOnlineCodexNodes());
        }
    }
}
