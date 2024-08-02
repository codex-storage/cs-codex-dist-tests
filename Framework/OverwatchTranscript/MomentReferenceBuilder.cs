using Logging;
using Newtonsoft.Json;

namespace OverwatchTranscript
{
    public class MomentReferenceBuilder
    {
        private const int MaxMomentsPerReference = 10000;
        private readonly ILog log;
        private readonly string workingDir;

        public MomentReferenceBuilder(ILog log, string workingDir)
        {
            this.log = log;
            this.workingDir = workingDir;
        }

        public OverwatchMomentReference[] Build(IFinalizedBucket[] finalizedBuckets)
        {
            var result = new List<OverwatchMomentReference>();
            var currentBuilder = new Builder(log, workingDir);

            var buckets = finalizedBuckets.ToList();
            log.Debug($"Building references for {buckets.Count} buckets.");
            while (buckets.Any())
            {
                buckets.RemoveAll(b => b.IsEmpty);
                if (!buckets.Any()) break;

                var earliestUtc = GetEarliestUtc(buckets);
                if (earliestUtc == null) continue;
                
                var tops = CollectAllTopsForUtc(earliestUtc.Value, buckets);
                var moment = ConvertTopsToMoment(tops);
                currentBuilder.Add(moment);
                if (currentBuilder.NumberOfMoments == MaxMomentsPerReference)
                {
                    result.Add(currentBuilder.Build());
                    currentBuilder = new Builder(log, workingDir);
                }
            }

            if (currentBuilder.NumberOfMoments > 0)
            {
                result.Add(currentBuilder.Build());
            }

            return result.ToArray();
        }

        private OverwatchMoment ConvertTopsToMoment(List<BucketTop> tops)
        {
            var discintUtc = tops.Select(e => e.Utc).Distinct().ToArray();
            if (discintUtc.Length != 1) throw new Exception("UTC mixing in moment construction.");

            return new OverwatchMoment
            {
                Utc = tops[0].Utc,
                Events = tops.SelectMany(e => e.Events).ToArray()
            };
        }

        private List<BucketTop> CollectAllTopsForUtc(DateTime earliestUtc, List<IFinalizedBucket> buckets)
        {
            var result = new List<BucketTop>();

            foreach (var bucket in buckets)
            {
                if (bucket.IsEmpty) continue;

                var utc = bucket.SeeTopUtc();
                if (utc == null) continue;

                if (utc.Value == earliestUtc)
                {
                    var top = bucket.TakeTop();
                    if (top == null) throw new Exception("top was null after top utc was not");
                    result.Add(top);
                }
            }

            return result;
        }

        private DateTime? GetEarliestUtc(List<IFinalizedBucket> buckets)
        {
            var earliest = DateTime.MaxValue;
            foreach (var bucket in buckets)
            {
                var utc = bucket.SeeTopUtc();
                if (utc == null) return null;

                if (utc.Value < earliest) earliest = utc.Value;
            }
            return earliest;
        }

        public class Builder
        {
            private readonly ILog log;
            private OverwatchMomentReference reference;
            private readonly ActionQueue queue = new ActionQueue();

            public Builder(ILog log, string workingDir)
            {
                reference = new OverwatchMomentReference
                {
                    MomentsFile = Path.Combine(workingDir, Guid.NewGuid().ToString()),
                    EarliestUtc = DateTime.MaxValue,
                    LatestUtc = DateTime.MinValue,
                    NumberOfEvents = 0,
                    NumberOfMoments = 0,
                };
                this.log = log;

                queue.Start();
            }

            public int NumberOfMoments => reference.NumberOfMoments;

            public void Add(OverwatchMoment moment)
            {
                if (moment.Utc < reference.EarliestUtc) reference.EarliestUtc = moment.Utc;
                if (moment.Utc > reference.LatestUtc) reference.LatestUtc = moment.Utc;
                reference.NumberOfMoments++;
                reference.NumberOfEvents += moment.Events.Length;

                queue.Add(() =>
                {
                    File.AppendAllLines(reference.MomentsFile, new[]
                    {
                        JsonConvert.SerializeObject(moment)
                    });
                });
            }

            public OverwatchMomentReference Build()
            {
                queue.StopAndJoin();

                log.Debug($"Created reference with {reference.NumberOfMoments} moments and {reference.NumberOfEvents} events...");
                var result = reference;
                reference = null!;
                return result;
            }
        }
    }
}
