using Utils;

namespace CodexPlugin
{
    public interface ITransferSpeeds
    {
        BytesPerSecond GetUploadSpeed();
        BytesPerSecond GetDownloadSpeed();
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

        public BytesPerSecond GetUploadSpeed()
        {
            return Average(uploads);
        }

        public BytesPerSecond GetDownloadSpeed()
        {
            return Average(downloads);
        }

        private static BytesPerSecond Convert(ByteSize size, TimeSpan duration)
        {
            double bytes = size.SizeInBytes;
            double seconds = duration.TotalSeconds;

            return new BytesPerSecond(System.Convert.ToInt64(Math.Round(bytes / seconds)));
        }

        private static BytesPerSecond Average(List<BytesPerSecond> list)
        {
            double sum = list.Sum(i => i.SizeInBytes);
            double num = list.Count;

            return new BytesPerSecond(System.Convert.ToInt64(Math.Round(sum / num)));
        }
    }
}
