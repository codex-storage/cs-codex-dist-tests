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

    public class Run
    {
        public Run(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; private set; }

        public bool Includes(int index)
        {
            return index >= Start && index < (Start + Length);
        }

        public RunUpdate ExpandToInclude(int index)
        {
            if (Includes(index)) throw new Exception("Run already includes this index. Run: {ToString()} index: {index}");
            if (index == (Start + Length))
            {
                Length++;
                return new RunUpdate();
            }
            if (index == (Start - 1))
            {
                return new RunUpdate(
                    newRuns: [new Run(Start - 1, Length + 1)],
                    removeRuns: [this]
                );
            }
            throw new Exception($"Run cannot expand to include index. Run: {ToString()} index: {index}");
        }

        public RunUpdate Unset(int index)
        {
            if (!Includes(index))
            {
                return new RunUpdate();
            }

            if (index == Start)
            {
                // First index: Replace self with new run at next index, unless empty.
                if (Length == 1)
                {
                    return new RunUpdate(Array.Empty<Run>(), new[] { this });
                }
                return new RunUpdate(
                    newRuns: [new Run(Start + 1, Length - 1)],
                    removeRuns: [this]
                );
            }

            if (index == (Start + Length - 1))
            {
                // Last index: Become one smaller.
                Length--;
                return new RunUpdate();
            }

            // Split:
            var newRunLength = (Start + Length - 1) - index;
            Length = index - Start;
            return new RunUpdate(
                newRuns: [new Run(index + 1, newRunLength)],
                removeRuns: Array.Empty<Run>()
            );
        }
        
        public void Iterate(Action<int> action)
        {
            for (var i = 0; i < Length; i++)
            {
                action(Start + i);
            }
        }

        public override string ToString()
        {
            return $"[{Start},{Length}]";
        }

        public override bool Equals(object? obj)
        {
            return obj is Run run &&
                   Start == run.Start &&
                   Length == run.Length;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, Length);
        }

        public static bool operator ==(Run? obj1, Run? obj2)
        {
            if (ReferenceEquals(obj1, obj2)) return true;
            if (ReferenceEquals(obj1, null)) return false;
            if (ReferenceEquals(obj2, null)) return false;
            return obj1.Equals(obj2);
        }
        public static bool operator !=(Run? obj1, Run? obj2) => !(obj1 == obj2);
    }

    public class RunUpdate
    {
        public RunUpdate()
            : this(Array.Empty<Run>(), Array.Empty<Run>())
        {
        }

        public RunUpdate(Run[] newRuns, Run[] removeRuns)
        {
            NewRuns = newRuns;
            RemoveRuns = removeRuns;
        }

        public Run[] NewRuns { get; }
        public Run[] RemoveRuns { get; }
    }
}
