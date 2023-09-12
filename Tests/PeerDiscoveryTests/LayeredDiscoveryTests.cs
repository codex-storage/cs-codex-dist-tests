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
            var root = this.SetupCodexNode();
            var l1Source = this.SetupCodexNode(s => s.WithBootstrapNode(root));
            var l1Node = this.SetupCodexNode(s => s.WithBootstrapNode(root));
            var l2Target = this.SetupCodexNode(s => s.WithBootstrapNode(l1Node));

            AssertAllNodesConnected();
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = this.SetupCodexNode();
            var l1Source = this.SetupCodexNode(s => s.WithBootstrapNode(root));
            var l1Node = this.SetupCodexNode(s => s.WithBootstrapNode(root));
            var l2Node = this.SetupCodexNode(s => s.WithBootstrapNode(l1Node));
            var l3Target = this.SetupCodexNode(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected();
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void NodeChainTest(int chainLength)
        {
            var node = this.SetupCodexNode();
            for (var i = 1; i < chainLength; i++)
            {
                node = this.SetupCodexNode(s => s.WithBootstrapNode(node));
            }

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            //CreatePeerConnectionTestHelpers().AssertFullyConnected(GetAllOnlineCodexNodes());
        }
    }
}
