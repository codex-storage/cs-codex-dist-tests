using NUnit.Framework;
using Utils;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class RunLengthEncodingTests
    {
        private readonly Random random = new Random();

        [Test]
        public void EmptySet()
        {
            var set = new IndexSet();
            for (var i = 0; i < 1000; i++)
            {
                Assert.That(set.IsSet(i), Is.False);
            }

            var calls = 0;
            set.Iterate(i => calls++);
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void SetsIndex()
        {
            var set = new IndexSet();
            var index = 1234;
            set.Set(index);

            Assert.That(set.IsSet(index), Is.True);
        }

        [Test]
        public void UnsetsIndex()
        {
            var set = new IndexSet();
            var index = 1234;
            set.Set(index);
            set.Unset(index);

            Assert.That(set.IsSet(index), Is.False);
        }

        [Test]
        public void RandomIndices()
        {
            var indices = GenerateRandomIndices();
            var set = new IndexSet(indices);

            AssertEqual(set, indices);
        }

        [Test]
        public void RandomRunLengthEncoding()
        {
            var indices = GenerateRandomIndices();
            var set = new IndexSet(indices);

            var encoded = set.RunLengthEncoded();
            var decoded = IndexSet.FromRunLengthEncoded(encoded);

            AssertEqual(decoded, indices);
        }

        [Test]
        public void RunLengthEncoding()
        {
            var indices = new[] { 0, 1, 2, 4, 6, 7 };
            var set = new IndexSet(indices);
            var encoded = set.RunLengthEncoded();

            CollectionAssert.AreEqual(new[]
            {
                0, 3,
                4, 1,
                6, 2
            }, encoded);
        }

        [Test]
        public void RunLengthDecoding()
        {
            var encoded = new[]
            {
                2, 4, // 2, 3, 4, 5
                7, 1, // 7
                9, 2  // 9, 10
            };

            var set = IndexSet.FromRunLengthEncoded(encoded);
            var seen = new List<int>();
            set.Iterate(i => seen.Add(i));

            CollectionAssert.AreEqual(new[]
            {
                2, 3, 4, 5,
                7,
                9, 10
            }, seen);
        }

        [Test]
        public void SetIndexBeforeRun()
        {
            var set = new IndexSet(new[] { 12, 13, 14 });
            set.Set(11);
            var encoded = set.RunLengthEncoded();

            CollectionAssert.AreEqual(new[]
            {
                11, 4
            }, encoded);
        }

        [Test]
        public void SetIndexBetweenRuns()
        {
            var set = new IndexSet(new[] {8, 9, 10, 12, 13, 14 });
            set.Set(11);
            var encoded = set.RunLengthEncoded();

            CollectionAssert.AreEqual(new[]
            {
                8, 7
            }, encoded);
        }

        [Test]
        public void SetIndexAfterRun()
        {
            var set = new IndexSet(new[] { 12, 13, 14 });
            set.Set(15);
            var encoded = set.RunLengthEncoded();

            CollectionAssert.AreEqual(new[]
            {
                12, 4
            }, encoded);
        }

        [Test]
        public void UnsetIndexAtStartOfRun()
        {
            var set = new IndexSet(new[] { 11, 12, 13, 14 });
            set.Unset(11);
            var encoded = set.RunLengthEncoded();

            CollectionAssert.AreEqual(new[]
            {
                12, 3
            }, encoded);
        }

        [Test]
        public void UnsetIndexAtEndOfRun()
        {
            var set = new IndexSet(new[] { 11, 12, 13, 14 });
            set.Unset(14);
            var encoded = set.RunLengthEncoded();

            CollectionAssert.AreEqual(new[]
            {
                11, 3
            }, encoded);
        }

        [Test]
        public void UnsetIndexInRun()
        {
            var set = new IndexSet(new[] { 11, 12, 13, 14 });
            set.Unset(12);
            var encoded = set.RunLengthEncoded();

            CollectionAssert.AreEqual(new[]
            {
                11, 1,
                13, 2
            }, encoded);
        }

        private void AssertEqual(IndexSet set, int[] indices)
        {
            var max = indices.Max() + 1;
            for (var i = 0; i < max; i++)
            {
                Assert.That(set.IsSet(i), Is.EqualTo(indices.Contains(i)));
            }

            var seen = new List<int>();
            set.Iterate(i => seen.Add(i));

            CollectionAssert.AreEqual(indices, seen);
        }

        private int[] GenerateRandomIndices()
        {
            var number = 1000;
            var max = 2000;
            var all = Enumerable.Range(0, max).ToList();
            var result = new List<int>();

            while (all.Any() && result.Count < number)
            {
                result.Add(all.PickOneRandom());
            }

            all.Sort();
            return all.ToArray();
        }
    }

    public partial class IndexSet
    {
        private readonly SortedList<int, Run> runs = new SortedList<int, Run>();

        public IndexSet()
        {
        }

        public IndexSet(int[] indices)
        {
            foreach (var i in indices) Set(i);
        }

        public static IndexSet FromRunLengthEncoded(int[] rle)
        {
            var set = new IndexSet();
            for (var i = 0; i < rle.Length; i += 2)
            {
                var start = rle[i];
                var length = rle[i + 1];
                set.runs.Add(start, new Run(start, length));
            }

            return set;
        }

        public bool IsSet(int index)
        {
            if (runs.ContainsKey(index)) return true;

            var run = GetRunAt(index);
            if (run == null) return false;
            return true;
        }

        public void Set(int index)
        {
            if (IsSet(index)) return;

            var runBefore = GetRunAt(index - 1);
            var runAfter = GetRunExact(index + 1);

            if (runBefore == null)
            {
                if (runAfter == null)
                {
                    CreateNewRun(index);
                }
                else
                {
                    HandleUpdate(runAfter.ExpandToInclude(index));
                }
            }
            else
            {
                if (runAfter == null)
                {
                    HandleUpdate(runBefore.ExpandToInclude(index));
                }
                else
                {
                    // new index will connect runBefore with runAfter. We merge!
                    HandleUpdate(new RunUpdate(
                        newRuns: [new Run(runBefore.Start, runBefore.Length + 1 + runAfter.Length)],
                        removeRuns: [runBefore, runAfter]
                    ));
                }
            }
        }

        public void Unset(int index)
        {
            if (runs.ContainsKey(index))
            {
                HandleUpdate(runs[index].Unset(index));
            }
            else
            {
                var run = GetRunAt(index);
                if (run == null) return;
                HandleUpdate(run.Unset(index));
            }
        }

        public void Iterate(Action<int> onIndex)
        {
            foreach (var run in runs.Values)
            {
                run.Iterate(onIndex);
            }
        }

        public int[] RunLengthEncoded()
        {
            return Encode().ToArray();
        }

        public override string ToString()
        {
            return string.Join("&", runs.Select(r => r.ToString()).ToArray());
        }

        private IEnumerable<int> Encode()
        {
            foreach (var pair in runs)
            {
                yield return pair.Value.Start;
                yield return pair.Value.Length;
            }
        }

        private Run? GetRunAt(int index)
        {
            foreach (var run in runs.Values)
            {
                if (run.Includes(index)) return run;
            }
            return null;
        }

        private Run? GetRunExact(int index)
        {
            if (runs.ContainsKey(index)) return runs[index];
            return null;
        }

        private void HandleUpdate(RunUpdate runUpdate)
        {
            foreach (var removeRun in runUpdate.RemoveRuns) runs.Remove(removeRun.Start);
            foreach (var newRun in runUpdate.NewRuns) runs.Add(newRun.Start, newRun);
        }

        private void CreateNewRun(int index)
        {
            if (runs.ContainsKey(index + 1))
            {
                var length = runs[index + 1].Length + 1;
                runs.Add(index, new Run(index, length));
                runs.Remove(index + 1);
            }
            else
            {
                runs.Add(index, new Run(index, 1));
            }
        }
    }
}
