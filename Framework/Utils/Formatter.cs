namespace Utils
{
    public static class Formatter
    {
        private static readonly string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        public static string FormatByteSize(long bytes)
        {
            if (bytes == 0) return "0" + sizeSuffixes[0];

            var sizeOrder = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var digit = Math.Round(bytes / Math.Pow(1024, sizeOrder), 1);
            return digit.ToString() + sizeSuffixes[sizeOrder];
        }
    }
}
