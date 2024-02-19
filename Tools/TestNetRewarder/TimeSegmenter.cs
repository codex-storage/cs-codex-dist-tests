using Logging;
using Utils;

namespace TestNetRewarder
{
    public class TimeSegmenter
    {
        private readonly ILog log;
        private readonly TimeSpan segmentSize;
        private DateTime start;

        public TimeSegmenter(ILog log, Configuration configuration)
        {
            this.log = log;

            if (configuration.Interval < 0) configuration.Interval = 15;
            if (configuration.CheckHistoryTimestamp == 0) throw new Exception("'check-history' unix timestamp is required. Set it to the start/launch moment of the testnet.");

            segmentSize = TimeSpan.FromSeconds(configuration.Interval);
            start = DateTimeOffset.FromUnixTimeSeconds(configuration.CheckHistoryTimestamp).UtcDateTime;

            log.Log("Starting time segments at " + start);
            log.Log("Segment size: " + Time.FormatDuration(segmentSize));
        }

        public async Task WaitForNextSegment(Func<TimeRange, Task> onSegment)
        {
            var now = DateTime.UtcNow;
            var end = start + segmentSize;
            var waited = false;
            if (end > now)
            {
                // Wait for the entire time segment to be in the past.
                var delay = (end - now).Add(TimeSpan.FromSeconds(3));
                waited = true;
                log.Log($"Waiting till time segment is in the past... {Time.FormatDuration(delay)}");
                await Task.Delay(delay, Program.CancellationToken);
            }

            if (Program.CancellationToken.IsCancellationRequested) return;

            var postfix = "(Catching up...)";
            if (waited) postfix = "(Real-time)";

            log.Log($"Time segment [{start} to {end}] {postfix}");
            var range = new TimeRange(start, end);
            start = end;

            await onSegment(range);
        }
    }
}
