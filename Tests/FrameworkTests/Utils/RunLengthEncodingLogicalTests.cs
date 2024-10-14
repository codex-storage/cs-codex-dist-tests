using NUnit.Framework;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class RunLengthEncodingLogicalTests
    {
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
            return this;
        }

        public IndexSet Merge(IndexSet other)
        {
            return this;
        }

        public IndexSet Without(IndexSet other)
        {
            return this;
        }

        public override bool Equals(object? obj)
        {
            return obj is IndexSet set &&
                   EqualityComparer<SortedList<int, Run>>.Default.Equals(runs, set.runs);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(runs);
        }
    }
}
