using DistTestCore;
using DistTestCore.Helpers;
using NUnit.Framework;
using Utils;

namespace Tests.PeerDiscoveryTests
{
    [TestFixture]
    public class LayeredDiscoveryTests : DistTest
    {
        [Test]
        public void TwoLayersTest()
        {
            var root = SetupCodexNode();
            var l1Source = SetupCodexNode(s => s.WithBootstrapNode(root));
            var l1Node = SetupCodexNode(s => s.WithBootstrapNode(root));
            var l2Target = SetupCodexNode(s => s.WithBootstrapNode(l1Node));

            AssertAllNodesConnected();
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = SetupCodexNode();
            var l1Source = SetupCodexNode(s => s.WithBootstrapNode(root));
            var l1Node = SetupCodexNode(s => s.WithBootstrapNode(root));
            var l2Node = SetupCodexNode(s => s.WithBootstrapNode(l1Node));
            var l3Target = SetupCodexNode(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected();
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(50)]
        public void NodeChainTest(int chainLength)
        {
            var node = SetupCodexNode();
            for (var i = 1; i < chainLength; i++)
            {
                node = SetupCodexNode(s => s.WithBootstrapNode(node));
            }

            AssertAllNodesConnected();

            for (int i = 0; i < 5; i++)
            {
                Time.Sleep(TimeSpan.FromSeconds(30));
                AssertAllNodesConnected();
            }
        }

        private void AssertAllNodesConnected()
        {
            PeerConnectionTestHelpers.AssertFullyConnected(GetAllOnlineCodexNodes());
            //PeerDownloadTestHelpers.AssertFullDownloadInterconnectivity(GetAllOnlineCodexNodes());
        }
    }
}
