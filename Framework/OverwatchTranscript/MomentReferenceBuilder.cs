using Logging;
using Newtonsoft.Json;

namespace OverwatchTranscript
{
    public class MomentReferenceBuilder
    {
        private const int MaxMomentsPerReference = 1000;
        private readonly ILog log;
        private readonly string workingDir;

        public MomentReferenceBuilder(ILog log, string workingDir)
        {
            this.log = log;
            this.workingDir = workingDir;
        }

        public OverwatchMomentReference[] Build(IFinalizedBucket[] buckets)
        {
            var result = new List<OverwatchMomentReference>();
            var currentBuilder = new Builder(log, workingDir);

            log.Debug($"Building references for {buckets.Length} buckets.");
            while (EntriesRemaining(buckets))
            {
                var earliestUtc = GetEarliestUtc(buckets);
                var entries = CollectAllEntriesForUtc(earliestUtc, buckets);
                var moment = ConvertEntriesToMoment(entries);
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

        private OverwatchMoment ConvertEntriesToMoment(List<EventBucketEntry> entries)
        {
            var discintUtc = entries.Select(e => e.Utc).Distinct().ToArray();
            if (discintUtc.Length != 1) throw new Exception("UTC mixing in moment construction.");

            return new OverwatchMoment
            {
                Utc = entries[0].Utc,
                Events = entries.Select(e => e.Event).ToArray()
            };
        }

        private List<EventBucketEntry> CollectAllEntriesForUtc(DateTime earliestUtc, IFinalizedBucket[] buckets)
        {
            var result = new List<EventBucketEntry>();

            foreach (var bucket in buckets)
            {
                var top = bucket.ViewTopEntry();
                while (top != null && top.Utc == earliestUtc)
                {
                    result.Add(top);
                    bucket.PopTopEntry();
                    top = bucket.ViewTopEntry();
                }
            }

            return result;
        }

        private DateTime GetEarliestUtc(IFinalizedBucket[] buckets)
        {
            var earliest = DateTime.MaxValue;
            foreach (var bucket in buckets)
            {
                var top = bucket.ViewTopEntry();
                if (top != null && top.Utc < earliest) earliest = top.Utc;
            }
            return earliest;
        }

        private bool EntriesRemaining(IFinalizedBucket[] buckets)
        {
            return buckets.Any(b => b.ViewTopEntry() != null);
        }

        public class Builder
        {
            private readonly ILog log;
            private OverwatchMomentReference reference;

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
            }

            public int NumberOfMoments => reference.NumberOfMoments;

            public void Add(OverwatchMoment moment)
            {
                File.AppendAllLines(reference.MomentsFile, new[]
                {
                    JsonConvert.SerializeObject(moment)
                });

                if (moment.Utc < reference.EarliestUtc) reference.EarliestUtc = moment.Utc;
                if (moment.Utc > reference.LatestUtc) reference.LatestUtc = moment.Utc;
                reference.NumberOfMoments++;
                reference.NumberOfEvents += moment.Events.Length;
            }

            public OverwatchMomentReference Build()
            {
                log.Debug($"Created reference with {reference.NumberOfMoments} moments and {reference.NumberOfEvents} events...");
                var result = reference;
                reference = null!;
                return result;
            }
        }
    }
}
