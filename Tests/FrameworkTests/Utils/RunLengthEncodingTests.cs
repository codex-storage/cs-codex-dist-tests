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
}
