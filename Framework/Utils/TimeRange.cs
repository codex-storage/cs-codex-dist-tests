namespace Utils
{
    public class TimeRange
    {
        public TimeRange(DateTime from, DateTime to)
        {
            if (from < to)
            {
                From = from;
                To = to;
            }
            else
            {
                From = to;
                To = from;
            }
            Duration = To - From;
        }

        public DateTime From { get; }
        public DateTime To { get; }
        public TimeSpan Duration { get; }

        public override string ToString()
        {
            return $"{Time.FormatTimestamp(From)} -> {Time.FormatTimestamp(To)} ({Time.FormatDuration(Duration)})";
        }
    }
}
