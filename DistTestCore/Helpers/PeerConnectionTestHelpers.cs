using DistTestCore.Codex;
using NUnit.Framework;
using Utils;

namespace DistTestCore.Helpers
{
    public class PeerConnectionTestHelpers
    {
        private readonly Random random = new Random();
        private readonly DistTest test;

        public PeerConnectionTestHelpers(DistTest test)
        {
            this.test = test;
        }

        public void AssertFullyConnected(IEnumerable<IOnlineCodexNode> nodes)
        {
            var n = nodes.ToArray();

            AssertFullyConnected(n);

            for (int i = 0; i < 5; i++)
            {
                Time.Sleep(TimeSpan.FromSeconds(30));
                AssertFullyConnected(n);
            }
        }

        private void AssertFullyConnected(IOnlineCodexNode[] nodes)
        {
            test.Log($"Asserting peers are fully-connected for nodes: '{string.Join(",", nodes.Select(n => n.GetName()))}'...");
            var entries = CreateEntries(nodes);
            var pairs = CreatePairs(entries);

            RetryWhilePairs(pairs, () =>
            {
                CheckAndRemoveSuccessful(pairs);
            });

            if (pairs.Any())
            {
                test.Log($"Unsuccessful! Peers are not fully-connected: {string.Join(",", nodes.Select(n => n.GetName()))}");
                Assert.Fail(string.Join(Environment.NewLine, pairs.Select(p => p.GetMessage())));
                test.Log(string.Join(Environment.NewLine, pairs.Select(p => p.GetMessage())));
            }
            else
            {
                test.Log($"Success! Peers are fully-connected: {string.Join(",", nodes.Select(n => n.GetName()))}");
            }
        }

        private static void RetryWhilePairs(List<Pair> pairs, Action action)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromMinutes(10);
            while (pairs.Any() && timeout > DateTime.UtcNow)
            {
                action();

                if (pairs.Any()) Time.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private void CheckAndRemoveSuccessful(List<Pair> pairs)
        {
            var checkTasks = pairs.Select(p => Task.Run(() =>
            {
                ApplyRandomDelay();
                p.Check();
            })).ToArray();

            Task.WaitAll(checkTasks);

            foreach (var pair in pairs.ToArray())
            {
                if (pair.Success)
                {
                    test.Log(pair.GetMessage());
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

        private void ApplyRandomDelay()
        {
            // Calling all the nodes all at the same time is not exactly nice.
            Time.Sleep(TimeSpan.FromMicroseconds(random.Next(10, 1000)));
        }

        public class Entry
        {
            public Entry(IOnlineCodexNode node)
            {
                Node = node;
                Response = node.GetDebugInfo();
            }

            public IOnlineCodexNode Node { get; }
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
                var ip = n.CodexAccess.Container.Pod.PodInfo.Ip;
                var discPort = n.CodexAccess.Container.Recipe.GetPortByTag(CodexContainerRecipe.DiscoveryPortTag);
                return $"{ip}:{discPort.Number}";
            }
        }

        public enum PeerConnectionState
        {
            Unknown,
            Connection,
            NoConnection,
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
            public PeerConnectionState AKnowsB { get; private set; }
            public PeerConnectionState BKnowsA { get; private set; }
            public bool Success { get { return AKnowsB == PeerConnectionState.Connection && BKnowsA == PeerConnectionState.Connection; } }

            public void Check()
            {
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

                if (Success)
                {
                    return $"{aName} and {bName} know each other.";
                }

                return $"[{aName}-->{bName}] = {AKnowsB} AND [{aName}<--{bName}] = {BKnowsA}";
            }

            private string GetTimePostfix()
            {
                var aName = A.Response.id;
                var bName = B.Response.id;

                return $" ({aName}->{bName}: {aToBTime.TotalMinutes} seconds, {bName}->{aName}: {bToATime.TotalSeconds} seconds)";
            }

            private static TimeSpan Measure(Action action)
            {
                var start = DateTime.UtcNow;
                action();
                return DateTime.UtcNow - start;
            }

            private PeerConnectionState Knows(Entry a, Entry b)
            {
                lock (a)
                {
                    var peerId = b.Response.id;

                    try
                    {
                        var response = a.Node.GetDebugPeer(peerId, timeout);
                        if (!response.IsPeerFound)
                        {
                            return PeerConnectionState.NoConnection;
                        }
                        if (!string.IsNullOrEmpty(response.peerId) && response.addresses.Any())
                        {
                            return PeerConnectionState.Connection;
                        }
                    }
                    catch
                    {
                    }

                    // Didn't get a conclusive answer. Try again later.
                    return PeerConnectionState.Unknown;
                }
            }
        }
    }
}
