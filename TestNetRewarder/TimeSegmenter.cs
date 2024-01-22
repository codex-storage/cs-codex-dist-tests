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
            segmentSize = TimeSpan.FromSeconds(configuration.Interval);
            if (configuration.CheckHistoryTimestamp != 0)
            {
                start = DateTimeOffset.FromUnixTimeSeconds(configuration.CheckHistoryTimestamp).UtcDateTime;
            }
            else
            {
                start = DateTime.UtcNow - segmentSize;
            }

            log.Log("Starting time segments at " + start);
            log.Log("Segment size: " + Time.FormatDuration(segmentSize));
        }

        public async Task WaitForNextSegment(Func<TimeRange, Task> onSegment)
        {
            var now = DateTime.UtcNow;
            var end = start + segmentSize;
            if (end > now)
            {
                // Wait for the entire time segment to be in the past.
                var delay = (end - now).Add(TimeSpan.FromSeconds(3));
                await Task.Delay(delay, Program.CancellationToken);
            }

            if (Program.CancellationToken.IsCancellationRequested) return;

            log.Log($"Time segment {start} to {end}");
            var range = new TimeRange(start, end);
            start = end;

            await onSegment(range);
        }
    }
}
