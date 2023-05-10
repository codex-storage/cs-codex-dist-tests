using DistTestCore.Codex;
using DistTestCore;
using NUnit.Framework;
using Utils;
using Logging;

namespace Tests.PeerDiscoveryTests
{
    public static class PeerTestHelpers
    {
        public static void AssertFullyConnected(IEnumerable<IOnlineCodexNode> nodes, BaseLog? log = null)
        {
            AssertFullyConnected(log, nodes.ToArray());
        }

        public static void AssertFullyConnected(BaseLog? log = null, params IOnlineCodexNode[] nodes)
        {
            Time.Retry(() =>
            {
                for (var x = 0; x < nodes.Length; x++)
                {
                    for (var y = x + 1; y < nodes.Length; y++)
                    {
                        AssertKnowEachother(nodes[x], nodes[y], log);
                    }
                }
            });
        }

        private static void AssertKnowEachother(IOnlineCodexNode a, IOnlineCodexNode b, BaseLog? log)
        {
            AssertKnowEachother(a.GetDebugInfo(), b.GetDebugInfo(), log);
        }

        private static void AssertKnowEachother(CodexDebugResponse a, CodexDebugResponse b, BaseLog? log)
        {
            AssertKnows(a, b, log);
            AssertKnows(b, a, log);
        }

        private static void AssertKnows(CodexDebugResponse a, CodexDebugResponse b, BaseLog? log)
        {
            //var enginePeers = string.Join(",", a.enginePeers.Select(p => p.peerId));
            //var switchPeers = string.Join(",", a.switchPeers.Select(p => p.peerId));
            var tableNodes = string.Join(",", a.table.nodes.Select(n => n.nodeId));

            if (log != null)
            {
                log.Debug($"{a.table.localNode.nodeId} is looking for {b.table.localNode.nodeId} in table-nodes [{tableNodes}]");
            }

            //Assert.That(a.enginePeers.Any(p => p.peerId == b.id), $"{a.id} was looking for '{b.id}' in engine-peers [{enginePeers}] but it was not found.");
            //Assert.That(a.switchPeers.Any(p => p.peerId == b.id), $"{a.id} was looking for '{b.id}' in switch-peers [{switchPeers}] but it was not found.");
            Assert.That(a.table.nodes.Any(n => n.nodeId == b.table.localNode.nodeId), $"{a.table.localNode.nodeId} was looking for '{b.table.localNode.nodeId}' in table-nodes [{tableNodes}] but it was not found.");
        }
    }
}
