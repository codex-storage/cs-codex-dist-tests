using Logging;
using Utils;

namespace TestNetRewarder
{
    public interface ITimeSegmentHandler
    {
        Task<TimeSegmentResponse> OnNewSegment(TimeRange timeRange);
    }

    public enum TimeSegmentResponse
    {
        OK,
        Underload,
        Overload
    }

    public class TimeSegmenter
    {
        private const int maxSegmentMult = 50;
        private readonly ILog log;
        private readonly ITimeSegmentHandler handler;
        private readonly TimeSpan segmentSize;
        private DateTime latest;
        private int currentSegmentMult = 1;

        public TimeSegmenter(ILog log, TimeSpan segmentSize, DateTime historyStartUtc, ITimeSegmentHandler handler)
        {
            this.log = log;
            this.handler = handler;
            this.segmentSize = segmentSize;
            latest = historyStartUtc;

            log.Log("Starting time segments at " + latest);
            log.Log("Segment size: " + Time.FormatDuration(segmentSize));
        }

        public bool IsRealtime { get; private set; } = false;

        public async Task ProcessNextSegment()
        {
            var end = GetNewSegmentEnd();
            IsRealtime = await WaitUntilTimeSegmentInPast(end);

            if (Program.CancellationToken.IsCancellationRequested) return;

            var postfix = "(Catching up...)";
            if (IsRealtime) postfix = "(Real-time)";
            log.Log($"Time segment [{latest} to {end}] {postfix}({currentSegmentMult}x)");
            
            var range = new TimeRange(latest, end);
            latest = end;

            var response = await handler.OnNewSegment(range);
            HandleResponse(response);
        }

        private DateTime GetNewSegmentEnd()
        {
            if (IsRealtime) return latest + segmentSize;
            var segment = segmentSize * currentSegmentMult;
            var end = latest + segment;
            if (end > DateTime.UtcNow) return DateTime.UtcNow + segmentSize;
            return end;
        }

        private void HandleResponse(TimeSegmentResponse response)
        {
            switch (response)
            {
                case TimeSegmentResponse.OK:
                    if (currentSegmentMult > 1) currentSegmentMult--;
                    break;
                case TimeSegmentResponse.Underload:
                    if (currentSegmentMult < maxSegmentMult) currentSegmentMult++;
                    break;
                case TimeSegmentResponse.Overload:
                    currentSegmentMult = 1;
                    break;
                default:
                    throw new Exception("Unknown response type: " + response);
            }
        }

        private async Task<bool> WaitUntilTimeSegmentInPast(DateTime end)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), Program.CancellationToken);

            var now = DateTime.UtcNow;
            while (end > now)
            {
                currentSegmentMult = 1;
                var delay = (end - now) + TimeSpan.FromSeconds(3);
                await Task.Delay(delay, Program.CancellationToken);
                return true;
            }
            return false;
        }
    }
}
