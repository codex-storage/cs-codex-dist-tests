using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace Tests.BasicTests
{
    [TestFixture]
    public class PeerTests : DistTest
    {
        [Test]
        public void TwoNodes()
        {
            var primary = SetupCodexNode();
            var secondary = SetupCodexNode(s => s.WithBootstrapNode(primary));

            AssertKnowEachother(primary, secondary);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void VariableNodes(int number)
        {
            var bootstrap = SetupCodexNode();
            var nodes = SetupCodexNodes(number, s => s.WithBootstrapNode(bootstrap));

            foreach (var node in nodes) AssertKnowEachother(node, bootstrap);

            for (var x = 0; x < number; x++)
            {
                for (var y = x + 1; y < number; y++)
                {
                    AssertKnowEachother(nodes[x], nodes[y]);
                }
            }
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
            var enginePeers = string.Join(",", a.enginePeers.Select(p => p.peerId));
            var switchPeers = string.Join(",", a.switchPeers.Select(p => p.peerId));

            Log.Debug($"Looking for {b.id} in engine-peers [{enginePeers}]");
            Log.Debug($"Looking for {b.id} in switch-peers [{switchPeers}]");

            Assert.That(a.enginePeers.Any(p => p.peerId == b.id), $"Expected peerId '{b.id}' not found in engine-peers [{enginePeers}]");
            Assert.That(a.switchPeers.Any(p => p.peerId == b.id), $"Expected peerId '{b.id}' not found in switch-peers [{switchPeers}]");
        }
    }
}
