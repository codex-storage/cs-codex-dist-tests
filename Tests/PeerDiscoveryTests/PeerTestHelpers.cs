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
                    if (log != null) log.Log(pair.GetMessage());
                }
                else
                {
                    pair.IncreaseTimeout();
                }
            }
        }

        private static Entry[] CreateEntries(IOnlineCodexNode[] nodes)
        {
            return nodes.Select(n => new Entry(n)).ToArray();
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
        }

        public class Pair
        {
            private TimeSpan timeout = TimeSpan.FromSeconds(60);
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

            public void IncreaseTimeout()
            {
                //timeout *= 2;
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
