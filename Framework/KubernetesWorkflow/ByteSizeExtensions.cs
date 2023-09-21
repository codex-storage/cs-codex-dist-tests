using Utils;

namespace KubernetesWorkflow
{
    public static class ByteSizeExtensions
    {
        public static string ToSuffixNotation(this ByteSize b)
        {
            long x = 1024;
            var map = new Dictionary<long, string>
            {
                { Pow(x, 4), "Ti" },
                { Pow(x, 3), "Gi" },
                { Pow(x, 2), "Mi" },
                { (x), "Ki" },
            };

            var bytes = b.SizeInBytes;
            foreach (var pair in map)
            {
                if (bytes > pair.Key)
                {
                    double bytesD = bytes;
                    double divD = pair.Key;
                    double numD = Math.Ceiling(bytesD / divD);
                    var v = Convert.ToInt64(numD);
                    return $"{v}{pair.Value}";
                }
            }

            return $"{bytes}";
        }

        private static long Pow(long x, int v)
        {
            long result = 1;
            for (var i = 0; i < v; i++) result *= x;
            return result;
        }
    }
}
