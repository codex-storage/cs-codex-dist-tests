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
            var entries = nodes.Select(n => new Entry(n)).ToArray();

            var failureMessags = new List<string>();
            for (var x = 0; x < entries.Length; x++)
            {
                for (var y = x + 1; y < entries.Length; y++)
                {
                    AssertKnowEachother(failureMessags, entries[x], entries[y], log);
                }
            }

            CollectionAssert.IsEmpty(failureMessags);
        }

        private static void AssertKnowEachother(List<string> failureMessags, Entry a, Entry b, BaseLog? log)
        {
            AssertKnows(failureMessags, a, b, log);
            AssertKnows(failureMessags, b, a, log);
        }

        private static void AssertKnows(List<string> failureMessags, Entry a, Entry b, BaseLog? log)
        {
            var peerId = b.Response.id;

            try
            {
                var response = GetDebugPeer(a.Node, peerId);
                if (string.IsNullOrEmpty(response.peerId) || !response.addresses.Any())
                {
                    failureMessags.Add($"{a.Response.id} did not know peer {peerId}");
                }
                else if (log != null)
                {
                    log.Log($"{a.Response.id} knows {peerId}.");
                }
            }
            catch
            {
                failureMessags.Add($"{a.Response.id} was unable to get 'debug/peer/{peerId}'.");
            }
        }

        private static CodexDebugPeerResponse GetDebugPeer(IOnlineCodexNode node, string peerId)
        {
            return Time.Retry(() => node.GetDebugPeer(peerId));
        }

        public class Entry
        {
            public Entry(IOnlineCodexNode node)
            {
                Node = node;
                Response = node.GetDebugInfo();
            }

            public IOnlineCodexNode Node { get ; }
            public CodexDebugResponse Response { get; }
        }
    }
}
