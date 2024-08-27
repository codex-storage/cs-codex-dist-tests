using Logging;
using NuGet.Frameworks;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Numerics;
using Utils;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class IndexEncodingTests
    {
        private readonly Random random = new Random();

        [Test]
        public void IndexRanging()
        {
            var log = new FileLog(Path.Combine(Environment.CurrentDirectory, nameof(IndexRanging) + ".log"));

            var reruns = 1;
            var tests = CreateTests().ToArray();
            log.Log($"Tests: {tests.Length}");
            foreach (var test in tests)
            {
                log.Log($"Running {test.GetName()}");
                Parallel.For(0, reruns, i =>
                {
                    RunTest(log, test);
                });
            }

            log.Log("Results:");
            foreach (var test in tests)
            {
                test.PrintResult(log);
            }
        }

        private void RunTest(FileLog log, IndexTest test)
        { 
            var blockPresence = new BlockPresence(log, test.NumIndices, test.PresenceFactor);
            var presentIndices = blockPresence.Present.ToArray();

            //Stopwatch.Measure(log, nameof(RunLengthEncode), () =>
            //{
                var run = RunLengthEncode(presentIndices);
                test.RunLengthEncodingLengths.Add(run.Length);
            //});

            //Stopwatch.Measure(log, nameof(FlipMapEncode), () =>
            //{
                var flipMap = FlipMapEncode(presentIndices, test.NumIndices);
                test.FlipMapLengths.Add(flipMap.Length);
            //});
        }

        private int[] RunLengthEncode(int[] indices)
        {
            var result = new List<int>();
            if (indices.Length == 0) return result.ToArray();

            var runValue = indices[0];
            var runStart = runValue;
            var runLength = 1;
            for (var i = 1; i < indices.Length; i++)
            {
                if (i >= indices.Length)
                {
                    result.Add(runStart);
                    result.Add(runLength);
                }
                else
                {
                    var nextValue = indices[i];
                    if (nextValue == runValue + 1)
                    {
                        runLength++;
                    }
                    else
                    {
                        result.Add(runStart);
                        result.Add(runLength);

                        runLength = 1;
                        runStart = nextValue;
                        runValue = nextValue;
                    }
                }
            }

            return result.ToArray();
        }

        private int[] FlipMapEncode(int[] presentIndices, int numIndices)
        {
            var flips = new List<int>();
            if (presentIndices.Length == 0) return flips.ToArray();

            var current = false;
            for (var i = 0; i < numIndices; i++)
            {
                var isPresent = presentIndices.Contains(i);
                if (current != isPresent)
                {
                    flips.Add(i);
                    current = isPresent;
                }
            }

            return flips.ToArray();
        }

        private IEnumerable<IndexTest> CreateTests()
        {
            //// 10,000,000 indices * 64k = 610 GB dataset
            //for (var numIndices = 1000; numIndices < 10000000; numIndices *= 100)
            //{
            //    for (float factor = 0.0f; factor < 1.0f; factor += 0.1f)
            //    {
            //        yield return new IndexTest(numIndices, factor);
            //    }
            //    yield return new IndexTest(numIndices, 1.0f);
            //}

            yield return new IndexTest(100000, 0.5f);
        }

        public class IndexTest
        {
            public IndexTest(int numIndices, float presenceFactor)
            {
                NumIndices = numIndices;
                PresenceFactor = presenceFactor;
            }

            public int NumIndices { get; }
            public float PresenceFactor { get; }

            public int BitMapLength => Convert.ToInt32(Math.Ceiling(NumIndices / 8.0));
            public int PresenceArrayLength => Convert.ToInt32(Math.Ceiling(NumIndices * PresenceFactor));
            public ConcurrentBag<int> RunLengthEncodingLengths { get; } = new ConcurrentBag<int>();
            public ConcurrentBag<int> FlipMapLengths { get; } = new ConcurrentBag<int>();

            public void PrintResult(ILog log)
            {
                log.Log(GetName());
                log.Log($"BitmapLength: {BitMapLength}");
                log.Log($"PresenceArrayLength: {PresenceArrayLength}");
                log.Log($"RunLength: {RunLengthEncodingLengths.Average()}");
                log.Log($"FlipMap: {FlipMapLengths.Average()}");
                log.Log("");
            }

            public string GetName()
            {
                return $"Test: {NumIndices} indices, {PresenceFactor * 100.0f}% present.";
            }
        }

        public class BlockPresence
        {
            public BlockPresence(ILog log, int length, float factor)
            {
                //Stopwatch.Measure(log, "Factoring", () =>
                //{
                    var all = Enumerable.Range(0, length).ToList();

                    float l = length;
                    var numPresent = Convert.ToInt32(Math.Round(l * factor));
                    if (numPresent >= length)
                    {
                        Present = all.ToArray();
                        return;
                    }

                    var present = new List<int>();
                    while (present.Count < numPresent)
                    {
                        present.Add(all.PickOneRandom());
                    }
                    present.Sort();
                    Present = present.ToArray();
                //});
            }

            public int[] Present { get; private set; } = Array.Empty<int>();
        }
    }
}
