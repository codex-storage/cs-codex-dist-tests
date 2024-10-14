using NUnit.Framework;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class RunLengthEncodingLogicalTests
    {
        [Test]
        public void EqualityTest()
        {
            var setA = new IndexSet([1, 2, 3, 4]);
            var setB = new IndexSet([1, 2, 3, 4]);

            Assert.That(setA, Is.EqualTo(setB));
            Assert.That(setA == setB);
        }

        [Test]
        public void InequalityTest1()
        {
            var setA = new IndexSet([1, 2, 4, 5]);
            var setB = new IndexSet([1, 2, 3, 4]);

            Assert.That(setA, Is.Not.EqualTo(setB));
            Assert.That(setA != setB);
        }

        [Test]
        public void InequalityTest2()
        {
            var setA = new IndexSet([1, 2, 3]);
            var setB = new IndexSet([1, 2, 3, 4]);

            Assert.That(setA, Is.Not.EqualTo(setB));
            Assert.That(setA != setB);
        }

        [Test]
        public void InequalityTest3()
        {
            var setA = new IndexSet([2, 3, 4, 5]);
            var setB = new IndexSet([1, 2, 3, 4]);

            Assert.That(setA, Is.Not.EqualTo(setB));
            Assert.That(setA != setB);
        }

        [Test]
        public void InequalityTest()
        {
            var setA = new IndexSet([2, 3, 4]);
            var setB = new IndexSet([1, 2, 3, 4]);

            Assert.That(setA, Is.Not.EqualTo(setB));
            Assert.That(setA != setB);
        }

        [Test]
        public void Overlap()
        {
            var setA = new IndexSet([1, 2, 3, 4, 5, 11, 14]);
            var setB = new IndexSet([3, 4, 5, 6, 7, 11, 12, 13]);
            var expectedSet = new IndexSet([3, 4, 5, 11]);

            var set = setA.Overlap(setB);

            Assert.That(set, Is.EqualTo(expectedSet));
        }

        [Test]
        public void Merge()
        {
            var setA = new IndexSet([1, 2, 3, 4, 5, 11, 14]);
            var setB = new IndexSet([3, 4, 5, 6, 7, 11, 12, 13]);
            var expectedSet = new IndexSet([1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14]);

            var set = setA.Merge(setB);

            Assert.That(set, Is.EqualTo(expectedSet));
        }

        [Test]
        public void Without()
        {
            var setA = new IndexSet([1, 2, 3, 4, 5, 11, 14]);
            var setB = new IndexSet([3, 4, 5, 6, 7, 11, 12, 13]);
            var expectedSet = new IndexSet([1, 2, 14]);

            var set = setA.Without(setB);

            Assert.That(set, Is.EqualTo(expectedSet));
        }
    }

    public partial class IndexSet
    {
        public IndexSet Overlap(IndexSet other)
        {
            var result = new IndexSet();
            Iterate(i =>
            {
                if (other.IsSet(i)) result.Set(i);
            });
            return result;
        }

        public IndexSet Merge(IndexSet other)
        {
            var result = new IndexSet();
            Iterate(result.Set);
            other.Iterate(result.Set);
            return result;
        }

        public IndexSet Without(IndexSet other)
        {
            var result = new IndexSet();
            Iterate(i =>
            {
                if (!other.IsSet(i)) result.Set(i);
            });
            return result;
        }

        public override bool Equals(object? obj)
        {
            if (obj is IndexSet set)
            {
                if (set.runs.Count != runs.Count) return false;
                foreach (var pair in runs)
                {
                    if (!set.runs.ContainsKey(pair.Key)) return false;
                    if (set.runs[pair.Key] != pair.Value) return false;
                }
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(runs);
        }

        public static bool operator ==(IndexSet? obj1, IndexSet? obj2)
        {
            if (ReferenceEquals(obj1, obj2)) return true;
            if (ReferenceEquals(obj1, null)) return false;
            if (ReferenceEquals(obj2, null)) return false;
            return obj1.Equals(obj2);
        }
        public static bool operator !=(IndexSet? obj1, IndexSet? obj2) => !(obj1 == obj2);
    }
}
