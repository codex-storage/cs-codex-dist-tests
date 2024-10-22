namespace FrameworkTests.Utils
{
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
                    return new RunUpdate(
                        newRuns: Array.Empty<Run>(),
                        removeRuns: [this]
                    );
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
