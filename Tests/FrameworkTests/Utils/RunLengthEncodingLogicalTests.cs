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
}
