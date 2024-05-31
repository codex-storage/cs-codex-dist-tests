namespace Utils
{
    public class BlockInterval
    {
        public BlockInterval(TimeRange timeRange, ulong from, ulong to)
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
            TimeRange = timeRange;
        }

        public ulong From { get; }
        public ulong To { get; }
        public TimeRange TimeRange { get; }

        public override string ToString()
        {
            return $"[{From} - {To}]";
        }
    }
}
