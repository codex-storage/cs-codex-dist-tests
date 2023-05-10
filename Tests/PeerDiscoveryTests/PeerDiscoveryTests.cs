using DistTestCore.Codex;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace Tests.PeerDiscoveryTests
{
    [TestFixture]
    public class PeerDiscoveryTests : AutoBootstrapDistTest
    {
        [Test]
        public void TwoNodes()
        {
            var node = SetupCodexNode();

            AssertKnowEachother(BootstrapNode, node);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void VariableNodes(int number)
        {
            var nodes = SetupCodexNodes(number);

            AssertFullyConnected(nodes);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void VariableNodesInPods(int number)
        {
            var bootstrap = SetupCodexBootstrapNode();

            var nodes = new List<IOnlineCodexNode>();
            for (var i = 0; i < number; i++)
            {
                nodes.Add(SetupCodexNode(s => s.WithBootstrapNode(bootstrap)));
            }

            AssertFullyConnected(nodes);
        }

        private void AssertFullyConnected(IEnumerable<IOnlineCodexNode> nodes)
        {
            Retry(() =>
            {
                var array = nodes.ToArray();

                foreach (var node in array) AssertKnowEachother(node, BootstrapNode);

                for (var x = 0; x < array.Length; x++)
                {
                    for (var y = x + 1; y < array.Length; y++)
                    {
                        AssertKnowEachother(array[x], array[y]);
                    }
                }
            });
        }

        private static void Retry(Action action)
        {
            try
            {
                action();
                return;
            }
            catch
            {
                Time.Sleep(TimeSpan.FromMinutes(1));
            }

            action();
        }

        private void AssertKnowEachother(IOnlineCodexNode a, IOnlineCodexNode b)
        {
            AssertKnowEachother(a.GetDebugInfo(), b.GetDebugInfo());
        }

        private void AssertKnowEachother(CodexDebugResponse a, CodexDebugResponse b)
        {
            AssertKnows(a, b);
            AssertKnows(b, a);
        }

        private void AssertKnows(CodexDebugResponse a, CodexDebugResponse b)
        {
            //var enginePeers = string.Join(",", a.enginePeers.Select(p => p.peerId));
            //var switchPeers = string.Join(",", a.switchPeers.Select(p => p.peerId));
            var tableNodes = string.Join(",", a.table.nodes.Select(n => n.nodeId));

            //Debug($"{a.id} is looking for {b.id} in engine-peers [{enginePeers}]");
            //Debug($"{a.id} is looking for {b.id} in switch-peers [{switchPeers}]");
            Debug($"{a.table.localNode.nodeId} is looking for {b.table.localNode.nodeId} in table-nodes [{tableNodes}]");

            //Assert.That(a.enginePeers.Any(p => p.peerId == b.id), $"{a.id} was looking for '{b.id}' in engine-peers [{enginePeers}] but it was not found.");
            //Assert.That(a.switchPeers.Any(p => p.peerId == b.id), $"{a.id} was looking for '{b.id}' in switch-peers [{switchPeers}] but it was not found.");
            Assert.That(a.table.nodes.Any(n => n.nodeId == b.table.localNode.nodeId), $"{a.table.localNode.nodeId} was looking for '{b.table.localNode.nodeId}' in table-nodes [{tableNodes}] but it was not found.");
        }
    }
}
