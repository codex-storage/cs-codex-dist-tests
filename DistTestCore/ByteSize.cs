using Utils;

namespace DistTestCore
{
    public class ByteSize
    {

        public ByteSize(long sizeInBytes)
        {
            if (sizeInBytes < 0) throw new ArgumentException("Cannot create ByteSize object with size less than 0. Was: " + sizeInBytes);
            SizeInBytes = sizeInBytes;
        }

        public long SizeInBytes { get; }

        public long ToMB()
        {
            return SizeInBytes / (1024 * 1024);
        }

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
            return Formatter.FormatByteSize(SizeInBytes);
        }
    }

    public static class ByteSizeIntExtensions
    {
        private const long Kilo = 1024;

        public static ByteSize Bytes(this long i)
        {
            return new ByteSize(i);
        }

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

        public static ByteSize Bytes(this int i)
        {
            return new ByteSize(i);
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
