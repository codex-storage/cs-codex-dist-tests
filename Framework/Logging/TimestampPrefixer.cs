using Utils;

namespace Logging
{
    public class TimestampPrefixer : LogPrefixer
    {
        public TimestampPrefixer(ILog backingLog) : base(backingLog)
        {
        }

        protected override string GetPrefix()
        {
            return $"[{Time.FormatTimestamp(DateTime.UtcNow)}]";
        }
    }
}
