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
            var aKnowsB = Knows(a, b);
            var bKnowsA = Knows(b, a);

            var message = GetMessage(a, b, aKnowsB, bKnowsA);

            if (log != null) log.Log(message);
            if (!aKnowsB || !bKnowsA) failureMessags.Add(message);
        }

        private static string GetMessage(Entry a, Entry b, bool aKnowsB, bool bKnowsA)
        {
            var aName = a.Response.id;
            var bName = b.Response.id;

            if (aKnowsB && bKnowsA)
            {
                return $"{aName} and {bName} know each other.";
            }
            if (aKnowsB)
            {
                return $"{aName} knows {bName}, but {bName} does not know {aName}";
            }
            if (bKnowsA)
            {
                return $"{bName} knows {aName}, but {aName} does not know {bName}";
            }
            return $"{aName} and {bName} don't know each other.";
        }

        private static bool Knows(Entry a, Entry b)
        {
            var peerId = b.Response.id;

            try
            {
                var response = GetDebugPeer(a.Node, peerId);
                if (!string.IsNullOrEmpty(response.peerId) && response.addresses.Any())
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static CodexDebugPeerResponse GetDebugPeer(IOnlineCodexNode node, string peerId)
        {
            return Time.Retry(() => node.GetDebugPeer(peerId), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(0.1));
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
