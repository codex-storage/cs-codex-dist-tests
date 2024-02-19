namespace Utils
{
    public class BlockRange
    {
        public BlockRange(ulong from, ulong to)
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
        }

        public ulong From { get; }
        public ulong To { get; }
    }
}
