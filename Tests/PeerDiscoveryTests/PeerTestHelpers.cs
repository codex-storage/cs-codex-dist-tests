using DistTestCore.Codex;
using DistTestCore;
using NUnit.Framework;
using Logging;
using Utils;

namespace Tests.PeerDiscoveryTests
{
    public static class PeerTestHelpers
    {
        private static readonly Random random = new Random();

        public static void AssertFullyConnected(IEnumerable<IOnlineCodexNode> nodes, BaseLog? log = null)
        {
            AssertFullyConnected(log, nodes.ToArray());
        }

        public static void AssertFullyConnected(BaseLog? log = null, params IOnlineCodexNode[] nodes)
        {
            var entries = CreateEntries(nodes);
            var pairs = CreatePairs(entries);

            RetryWhilePairs(pairs, () =>
            {
                CheckAndRemoveSuccessful(pairs, log);
            });
            
            if (pairs.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, pairs.Select(p => p.GetMessage())));
            }
        }

        private static void RetryWhilePairs(List<Pair> pairs, Action action)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            while (pairs.Any() && (timeout > DateTime.UtcNow))
            {
                action();

                if (pairs.Any()) Time.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private static void CheckAndRemoveSuccessful(List<Pair> pairs, BaseLog? log)
        {
            var checkTasks = pairs.Select(p => Task.Run(p.Check)).ToArray();
            Task.WaitAll(checkTasks);

            foreach (var pair in pairs.ToArray())
            {
                if (pair.Success)
                {
                    pairs.Remove(pair);
                }
            }
        }

        private static Entry[] CreateEntries(IOnlineCodexNode[] nodes)
        {
            var entries = nodes.Select(n => new Entry(n)).ToArray();
            var incorrectDiscoveryEndpoints = entries.SelectMany(e => e.GetInCorrectDiscoveryEndpoints(entries)).ToArray();
           
            if (incorrectDiscoveryEndpoints.Any())
            {
                Assert.Fail("Some nodes contain peer records with incorrect discovery ip/port information: " +
                    string.Join(Environment.NewLine, incorrectDiscoveryEndpoints));
            }

            return entries;
        }

        private static List<Pair> CreatePairs(Entry[] entries)
        {
            return CreatePairsIterator(entries).ToList();
        }

        private static IEnumerable<Pair> CreatePairsIterator(Entry[] entries)
        {
            for (var x = 0; x < entries.Length; x++)
            {
                for (var y = x + 1; y < entries.Length; y++)
                {
                    yield return new Pair(entries[x], entries[y]);
                }
            }
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

            public IEnumerable<string> GetInCorrectDiscoveryEndpoints(Entry[] allEntries)
            {
                foreach (var peer in Response.table.nodes)
                {
                    var expected = GetExpectedDiscoveryEndpoint(allEntries, peer);
                    if (expected != peer.address)
                    {
                        yield return $"Node:{Node.GetName()} has incorrect peer table entry. Was: '{peer.address}', expected: '{expected}'";
                    }
                }
            }

            private static string GetExpectedDiscoveryEndpoint(Entry[] allEntries, CodexDebugTableNodeResponse node)
            {
                var peer = allEntries.SingleOrDefault(e => e.Response.table.localNode.peerId == node.peerId);
                if (peer == null) return $"peerId: {node.peerId} is not known.";

                var n = (OnlineCodexNode)peer.Node;
                var ip = n.CodexAccess.Container.Pod.Ip;
                var discPort = n.CodexAccess.Container.Recipe.GetPortByTag(CodexContainerRecipe.DiscoveryPortTag);
                return $"{ip}:{discPort.Number}";
            }
        }

        public class Pair
        {
            private readonly TimeSpan timeout = TimeSpan.FromSeconds(60);
            private TimeSpan aToBTime = TimeSpan.FromSeconds(0);
            private TimeSpan bToATime = TimeSpan.FromSeconds(0);

            public Pair(Entry a, Entry b)
            {
                A = a;
                B = b;
            }

            public Entry A { get; }
            public Entry B { get; }
            public bool AKnowsB { get; private set; }
            public bool BKnowsA { get; private set; }
            public bool Success {  get { return AKnowsB && BKnowsA; } }

            public void Check()
            {
                ApplyRandomDelay();
                aToBTime = Measure(() => AKnowsB = Knows(A, B));
                bToATime = Measure(() => BKnowsA = Knows(B, A));
            }

            public string GetMessage()
            {
                return GetResultMessage() + GetTimePostfix();
            }

            private string GetResultMessage()
            {
                var aName = A.Response.id;
                var bName = B.Response.id;

                if (AKnowsB && BKnowsA)
                {
                    return $"{aName} and {bName} know each other.";
                }
                if (AKnowsB)
                {
                    return $"{aName} knows {bName}, but {bName} does not know {aName}";
                }
                if (BKnowsA)
                {
                    return $"{bName} knows {aName}, but {aName} does not know {bName}";
                }
                return $"{aName} and {bName} don't know each other.";
            }

            private string GetTimePostfix()
            {
                var aName = A.Response.id;
                var bName = B.Response.id;

                return $" ({aName}->{bName}: {aToBTime.TotalMinutes} seconds, {bName}->{aName}: {bToATime.TotalSeconds} seconds)";
            }

            private static void ApplyRandomDelay()
            {
                // Calling all the nodes all at the same time is not exactly nice.
                Time.Sleep(TimeSpan.FromMicroseconds(random.Next(10, 100)));
            }

            private static TimeSpan Measure(Action action)
            {
                var start = DateTime.UtcNow;
                action();
                return DateTime.UtcNow - start;
            }

            private bool Knows(Entry a, Entry b)
            {
                lock (a)
                {
                    var peerId = b.Response.id;

                    try
                    {
                        var response = a.Node.GetDebugPeer(peerId, timeout);
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
            }
        }
    }
}
