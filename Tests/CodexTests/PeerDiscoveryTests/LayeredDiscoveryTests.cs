using CodexPlugin;
using CodexTests;
using NUnit.Framework;

namespace Tests.PeerDiscoveryTests
{
    [TestFixture]
    public class LayeredDiscoveryTests : CodexDistTest
    {
        [Test]
        public void TwoLayersTest()
        {
            var root = Ci.StartCodexNode();
            var l1Source = Ci.StartCodexNode(s => s.WithBootstrapNode(root));
            var l1Node = Ci.StartCodexNode(s => s.WithBootstrapNode(root));
            var l2Target = Ci.StartCodexNode(s => s.WithBootstrapNode(l1Node));

            AssertAllNodesConnected();
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = Ci.StartCodexNode();
            var l1Source = Ci.StartCodexNode(s => s.WithBootstrapNode(root));
            var l1Node = Ci.StartCodexNode(s => s.WithBootstrapNode(root));
            var l2Node = Ci.StartCodexNode(s => s.WithBootstrapNode(l1Node));
            var l3Target = Ci.StartCodexNode(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected();
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void NodeChainTest(int chainLength)
        {
            var node = Ci.StartCodexNode();
            for (var i = 1; i < chainLength; i++)
            {
                node = Ci.StartCodexNode(s => s.WithBootstrapNode(node));
            }

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            CreatePeerConnectionTestHelpers().AssertFullyConnected(GetAllOnlineCodexNodes());
        }
    }
}
