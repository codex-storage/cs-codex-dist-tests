namespace Utils
{
    public class BlockInterval
    {
        public BlockInterval(ulong from, ulong to)
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

        public override string ToString()
        {
            return $"[{From} - {To}]";
        }
    }
}
