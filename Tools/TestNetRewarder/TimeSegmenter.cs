using Logging;
using Utils;

namespace TestNetRewarder
{
    public interface ITimeSegmentHandler
    {
        Task OnNewSegment(TimeRange timeRange);
    }

    public class TimeSegmenter
    {
        private readonly ILog log;
        private readonly ITimeSegmentHandler handler;
        private readonly TimeSpan segmentSize;
        private DateTime latest;

        public TimeSegmenter(ILog log, Configuration configuration, ITimeSegmentHandler handler)
        {
            this.log = log;
            this.handler = handler;
            if (configuration.IntervalMinutes < 0) configuration.IntervalMinutes = 1;

            segmentSize = configuration.Interval;
            latest = configuration.HistoryStartUtc;

            log.Log("Starting time segments at " + latest);
            log.Log("Segment size: " + Time.FormatDuration(segmentSize));
        }

        public async Task ProcessNextSegment()
        {
            var end = latest + segmentSize;
            var waited = await WaitUntilTimeSegmentInPast(end);

            if (Program.CancellationToken.IsCancellationRequested) return;

            var postfix = "(Catching up...)";
            if (waited) postfix = "(Real-time)";
            log.Log($"Time segment [{latest} to {end}] {postfix}");
            
            var range = new TimeRange(latest, end);
            latest = end;

            await handler.OnNewSegment(range);
        }

        private async Task<bool> WaitUntilTimeSegmentInPast(DateTime end)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), Program.CancellationToken);

            var now = DateTime.UtcNow;
            while (end > now)
            {
                var delay = (end - now) + TimeSpan.FromSeconds(3);
                await Task.Delay(delay, Program.CancellationToken);
                return true;
            }
            return false;
        }
    }
}
