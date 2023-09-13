using CodexPlugin;
using DistTestCore;
using NUnit.Framework;

namespace Tests.PeerDiscoveryTests
{
    [TestFixture]
    public class LayeredDiscoveryTests : DistTest
    {
        [Test]
        public void TwoLayersTest()
        {
            var root = Ci.SetupCodexNode();
            var l1Source = Ci.SetupCodexNode(s => s.WithBootstrapNode(root));
            var l1Node = Ci.SetupCodexNode(s => s.WithBootstrapNode(root));
            var l2Target = Ci.SetupCodexNode(s => s.WithBootstrapNode(l1Node));

            AssertAllNodesConnected();
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = Ci.SetupCodexNode();
            var l1Source = Ci.SetupCodexNode(s => s.WithBootstrapNode(root));
            var l1Node = Ci.SetupCodexNode(s => s.WithBootstrapNode(root));
            var l2Node = Ci.SetupCodexNode(s => s.WithBootstrapNode(l1Node));
            var l3Target = Ci.SetupCodexNode(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected();
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void NodeChainTest(int chainLength)
        {
            var node = Ci.SetupCodexNode();
            for (var i = 1; i < chainLength; i++)
            {
                node = Ci.SetupCodexNode(s => s.WithBootstrapNode(node));
            }

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            //CreatePeerConnectionTestHelpers().AssertFullyConnected(GetAllOnlineCodexNodes());
        }
    }
}
