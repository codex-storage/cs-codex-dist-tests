using Utils;

namespace CodexPlugin
{
    public interface ITransferSpeeds
    {
        BytesPerSecond? GetUploadSpeed();
        BytesPerSecond? GetDownloadSpeed();
    }

    public class TransferSpeeds : ITransferSpeeds
    {
        private readonly List<BytesPerSecond> uploads = new List<BytesPerSecond>();
        private readonly List<BytesPerSecond> downloads = new List<BytesPerSecond>();

        public void AddUploadSample(ByteSize bytes, TimeSpan duration)
        {
            uploads.Add(Convert(bytes, duration));
        }

        public void AddDownloadSample(ByteSize bytes, TimeSpan duration)
        {
            downloads.Add(Convert(bytes, duration));
        }

        public BytesPerSecond? GetUploadSpeed()
        {
            if (!uploads.Any()) return null;
            return uploads.Average();
        }

        public BytesPerSecond? GetDownloadSpeed()
        {
            if (!downloads.Any()) return null;
            return downloads.Average();
        }

        private static BytesPerSecond Convert(ByteSize size, TimeSpan duration)
        {
            double bytes = size.SizeInBytes;
            double seconds = duration.TotalSeconds;

            return new BytesPerSecond(System.Convert.ToInt64(Math.Round(bytes / seconds)));
        }
    }

    public static class ListExtensions
    {
        public static BytesPerSecond Average(this List<BytesPerSecond> list)
        {
            double sum = list.Sum(i => i.SizeInBytes);
            double num = list.Count;

            return new BytesPerSecond(Convert.ToInt64(Math.Round(sum / num)));
        }

        public static BytesPerSecond? OptionalAverage(this List<BytesPerSecond?>? list)
        {
            if (list == null || !list.Any() || !list.Any(i => i != null)) return null;
            var values = list.Where(i => i != null).Cast<BytesPerSecond>().ToArray();
            double sum = values.Sum(i => i.SizeInBytes);
            double num = values.Length;

            return new BytesPerSecond(Convert.ToInt64(Math.Round(sum / num)));
        }
    }
}
