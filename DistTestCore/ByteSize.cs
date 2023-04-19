namespace DistTestCore
{
    public class ByteSize
    {
        public ByteSize(long sizeInBytes)
        {
            SizeInBytes = sizeInBytes;
        }

        public long SizeInBytes { get; }

        public override bool Equals(object? obj)
        {
            return obj is ByteSize size && SizeInBytes == size.SizeInBytes;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SizeInBytes);
        }

        public override string ToString()
        {
            return $"{SizeInBytes} bytes";
        }
    }

    public static class ByteSizeIntExtensions
    {
        private const long Kilo = 1024;

        public static ByteSize KB(this long i)
        {
            return new ByteSize(i * Kilo);
        }

        public static ByteSize MB(this long i)
        {
            return (i * Kilo).KB();
        }

        public static ByteSize GB(this long i)
        {
            return (i * Kilo).MB();
        }

        public static ByteSize TB(this long i)
        {
            return (i * Kilo).GB();
        }

        public static ByteSize KB(this int i)
        {
            return Convert.ToInt64(i).KB();
        }

        public static ByteSize MB(this int i)
        {
            return Convert.ToInt64(i).MB();
        }

        public static ByteSize GB(this int i)
        {
            return Convert.ToInt64(i).GB();
        }

        public static ByteSize TB(this int i)
        {
            return Convert.ToInt64(i).TB();
        }
    }
}
