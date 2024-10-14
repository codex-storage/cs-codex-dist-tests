using NUnit.Framework;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class RunLengthEncodingRunTests
    {
        [Test]
        public void EqualityTest()
        {
            var runA = new Run(1, 4);
            var runB = new Run(1, 4);

            Assert.That(runA, Is.EqualTo(runB));
            Assert.That(runA == runB);
        }

        [Test]
        public void InequalityTest1()
        {
            var runA = new Run(1, 4);
            var runB = new Run(1, 5);

            Assert.That(runA, Is.Not.EqualTo(runB));
            Assert.That(runA != runB);
        }

        [Test]
        public void InequalityTest2()
        {
            var runA = new Run(1, 4);
            var runB = new Run(2, 4);

            Assert.That(runA, Is.Not.EqualTo(runB));
            Assert.That(runA != runB);
        }

        [Test]
        [Combinatorial]
        public void RunIncludes(
            [Values(0, 1, 2, 3)] int start,
            [Values(1, 2, 3, 4)] int length)
        {
            var run = new Run(start, length);

            var shouldInclude = Enumerable.Range(start, length).ToArray();
            var shouldExclude = new int[]
            {
                shouldInclude.Min() - 1,
                shouldInclude.Max() + 1
            };

            foreach (var incl in shouldInclude)
            {
                Assert.That(run.Includes(incl));
            }
            foreach (var excl in shouldExclude)
            {
                Assert.That(!run.Includes(excl));
            }
        }

        [Test]
        public void RunExpandThrowsWhenIndexNotAdjacent()
        {
            var run = new Run(2, 3);
            Assert.That(!run.Includes(1));
            Assert.That(run.Includes(2));
            Assert.That(run.Includes(4));
            Assert.That(!run.Includes(5));

            Assert.That(() => run.ExpandToInclude(0), Throws.TypeOf<Exception>());
            Assert.That(() => run.ExpandToInclude(6), Throws.TypeOf<Exception>());
        }

        [Test]
        public void RunExpandThrowsWhenIndexAlreadyIncluded()
        {
            var run = new Run(2, 3);
            Assert.That(!run.Includes(1));
            Assert.That(run.Includes(2));
            Assert.That(run.Includes(4));
            Assert.That(!run.Includes(5));

            Assert.That(() => run.ExpandToInclude(2), Throws.TypeOf<Exception>());
            Assert.That(() => run.ExpandToInclude(3), Throws.TypeOf<Exception>());
        }

        [Test]
        public void RunExpandToIncludeAfter()
        {
            var run = new Run(2, 3);
            var update = run.ExpandToInclude(5);
            Assert.That(update, Is.Not.Null);
            Assert.That(update.NewRuns.Length, Is.EqualTo(0));
            Assert.That(update.RemoveRuns.Length, Is.EqualTo(0));
            Assert.That(run.Includes(5));
            Assert.That(!run.Includes(6));
        }

        [Test]
        public void RunExpandToIncludeBefore()
        {
            var run = new Run(2, 3);
            var update = run.ExpandToInclude(1);

            Assert.That(update, Is.Not.Null);
            Assert.That(update.NewRuns.Length, Is.EqualTo(1));
            Assert.That(update.RemoveRuns.Length, Is.EqualTo(1));

            Assert.That(update.RemoveRuns[0], Is.SameAs(run));
            Assert.That(update.NewRuns[0].Start, Is.EqualTo(1));
            Assert.That(update.NewRuns[0].Length, Is.EqualTo(4));
        }

        [Test]
        public void RunCanUnsetLastIndex()
        {
            var run = new Run(0, 3);
            Assert.That(run.Includes(2));
            var update = run.Unset(2);
            Assert.That(!run.Includes(2));

            Assert.That(update.NewRuns.Length, Is.EqualTo(0));
            Assert.That(update.RemoveRuns.Length, Is.EqualTo(0));
        }

        [Test]
        public void RunCanSplit()
        {
            var run = new Run(0, 6); // 0, 1, 2, 3, 4, 5
            var update = run.Unset(2);

            Assert.That(run.Start, Is.EqualTo(0));
            Assert.That(run.Length, Is.EqualTo(2)); // 0, 1
            Assert.That(!run.Includes(2));

            Assert.That(update.NewRuns.Length, Is.EqualTo(1));
            Assert.That(update.RemoveRuns.Length, Is.EqualTo(0));

            Assert.That(!update.NewRuns[0].Includes(2));
            Assert.That(update.NewRuns[0].Start, Is.EqualTo(3));
            Assert.That(update.NewRuns[0].Length, Is.EqualTo(3)); // 3, 4, 5
            Assert.That(!update.NewRuns[0].Includes(6));
        }

        [Test]
        public void RunReplacesSelfWhenUnsetFirstIndex()
        {
            var run = new Run(0, 5);
            var update = run.Unset(0);

            Assert.That(update.NewRuns.Length, Is.EqualTo(1));
            Assert.That(update.RemoveRuns.Length, Is.EqualTo(1));

            Assert.That(update.RemoveRuns[0], Is.SameAs(run));
            Assert.That(update.NewRuns[0].Start, Is.EqualTo(1));
            Assert.That(update.NewRuns[0].Length, Is.EqualTo(4));
        }

        [Test]
        public void CanIterateIndices()
        {
            var run = new Run(2, 4);
            var seen = new List<int>();
            run.Iterate(seen.Add);

            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, seen);
        }
    }
}
