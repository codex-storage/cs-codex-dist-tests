using CodexClient;
using CodexTests;
using NUnit.Framework;

namespace ExperimentalTests.PeerDiscoveryTests
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

            AssertAllNodesConnected(root, l1Source, l1Node, l2Target);
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = StartCodex();
            var l1Source = StartCodex(s => s.WithBootstrapNode(root));
            var l1Node = StartCodex(s => s.WithBootstrapNode(root));
            var l2Node = StartCodex(s => s.WithBootstrapNode(l1Node));
            var l3Target = StartCodex(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected(root, l1Source, l1Node, l2Node, l3Target);
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        public void NodeChainTest(int chainLength)
        {
            var nodes = new List<ICodexNode>();
            var node = StartCodex();
            nodes.Add(node);

            for (var i = 1; i < chainLength; i++)
            {
                node = StartCodex(s => s.WithBootstrapNode(node));
                nodes.Add(node);
            }

            AssertAllNodesConnected(nodes.ToArray());
        }

        private void AssertAllNodesConnected(params ICodexNode[] nodes)
        {
            CreatePeerConnectionTestHelpers().AssertFullyConnected(nodes);
        }
    }
}
