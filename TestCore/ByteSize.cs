namespace CodexDistTests.TestCore
{
    public class ByteSize
    {
        public ByteSize(long sizeInBytes)
        {
            SizeInBytes = sizeInBytes;
        }

        public long SizeInBytes { get; }
    }

    public static class IntExtensions
    {
        private const long Kilo = 1024;

        public static ByteSize KB(this long i)
        {
            return new ByteSize(i * Kilo);
        }

        public static ByteSize MB(this long i)
        {
            return KB(i * Kilo);
        }

        public static ByteSize GB(this long i)
        {
            return MB(i * Kilo);
        }

        public static ByteSize TB(this long i)
        {
            return GB(i * Kilo);
        }

        public static ByteSize KB(this int i)
        {
            return KB(Convert.ToInt64(i));
        }

        public static ByteSize MB(this int i)
        {
            return MB(Convert.ToInt64(i));
        }

        public static ByteSize GB(this int i)
        {
            return GB(Convert.ToInt64(i));
        }

        public static ByteSize TB(this int i)
        {
            return TB(Convert.ToInt64(i));
        }
    }
}
