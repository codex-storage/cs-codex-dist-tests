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
                var infos = nodes.Select(n => FetchDebugInfo(n, log)).ToArray();

                var failureMessags = new List<string>();
                for (var x = 0; x < infos.Length; x++)
                {
                    for (var y = x + 1; y < infos.Length; y++)
                    {
                        AssertKnowEachother(failureMessags, infos[x], infos[y], log);
                    }
                }

                CollectionAssert.IsEmpty(failureMessags);
            });
        }

        private static CodexDebugResponse FetchDebugInfo(IOnlineCodexNode n, BaseLog? log)
        {
            var info = n.GetDebugInfo();

            if (log != null)
            {
                log.AddStringReplace(info.table.localNode.nodeId, $"-<{n.GetName()}>-");
            }

            return info;
        }

        private static void AssertKnowEachother(List<string> failureMessags, CodexDebugResponse a, CodexDebugResponse b, BaseLog? log)
        {
            AssertKnows(failureMessags, a, b, log);
            AssertKnows(failureMessags, b, a, log);
        }

        private static void AssertKnows(List<string> failureMessags, CodexDebugResponse a, CodexDebugResponse b, BaseLog? log)
        {
            //var enginePeers = string.Join(",", a.enginePeers.Select(p => p.peerId));
            //var switchPeers = string.Join(",", a.switchPeers.Select(p => p.peerId));
            var tableNodes = string.Join(",", a.table.nodes.Select(n => n.nodeId));

            var success = a.table.nodes.Any(n => n.nodeId == b.table.localNode.nodeId);

            if (log != null)
            {
                var msg = success ? "PASS" : "FAIL";
                log.Log($"{msg} {a.table.localNode.nodeId} is looking for {b.table.localNode.nodeId} in table-nodes [{tableNodes}]");
            }

            //Assert.That(a.enginePeers.Any(p => p.peerId == b.id), $"{a.id} was looking for '{b.id}' in engine-peers [{enginePeers}] but it was not found.");
            //Assert.That(a.switchPeers.Any(p => p.peerId == b.id), $"{a.id} was looking for '{b.id}' in switch-peers [{switchPeers}] but it was not found.");
            if (!success)
            {
                failureMessags.Add($"{a.table.localNode.nodeId} was looking for '{b.table.localNode.nodeId}' in table-nodes [{tableNodes}] but it was not found.");
            }
        }
    }
}
