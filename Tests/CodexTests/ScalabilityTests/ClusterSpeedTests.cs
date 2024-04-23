using DistTestCore;
using Logging;
using NUnit.Framework;
using Utils;

namespace CodexTests.ScalabilityTests
{
    [TestFixture]
    public class ClusterDiscSpeedTests : DistTest
    {
        private readonly Random random = new Random();

        [Test]
        [Combinatorial]
        public void DiscSpeedTest(
            [Values(1, 10, 100, 1024, 1024 * 10, 1024 * 100, 1024 * 1024)] int bufferSizeKb
        )
        {
            long targetSize = (long)(1024 * 1024 * 1024) * 2;
            long bufferSizeBytes = ((long)bufferSizeKb) * 1024;

            var filename = nameof(DiscSpeedTest);

            Thread.Sleep(1000);
            if (File.Exists(filename)) File.Delete(filename);
            Thread.Sleep(1000);
            var writeSpeed = PerformWrite(targetSize, bufferSizeBytes, filename);
            Thread.Sleep(1000);
            var readSpeed = PerformRead(targetSize, bufferSizeBytes, filename);
            
            Log($"Write speed: {writeSpeed} per second.");
            Log($"Read speed: {writeSpeed} per second.");
        }

        private ByteSize PerformWrite(long targetSize, long bufferSizeBytes, string filename)
        {
            long bytesWritten = 0;
            var buffer = new byte[bufferSizeBytes];
            random.NextBytes(buffer);

            var sw = Stopwatch.Begin(GetTestLog());
            using (var stream = File.OpenWrite(filename))
            {
                while (bytesWritten < targetSize)
                {
                    long remaining = targetSize - bytesWritten;
                    long toWrite = Math.Min(bufferSizeBytes, remaining);

                    stream.Write(buffer, 0, Convert.ToInt32(toWrite));
                    bytesWritten += toWrite;
                }
            }
            var duration = sw.End("WriteTime");
            double totalSeconds = duration.TotalSeconds;
            double totalBytes = bytesWritten;
            double bytesPerSecond = totalBytes / totalSeconds;
            return new ByteSize(Convert.ToInt64(bytesPerSecond));
        }

        private ByteSize PerformRead(long targetSize, long bufferSizeBytes, string filename)
        {
            long bytesRead = 0;
            var buffer = new byte[bufferSizeBytes];
            var sw = Stopwatch.Begin(GetTestLog());
            using (var stream = File.OpenRead(filename))
            {
                while (bytesRead < targetSize)
                {
                    long remaining = targetSize - bytesRead;
                    long toRead = Math.Min(bufferSizeBytes, remaining);

                    var r = stream.Read(buffer, 0, Convert.ToInt32(toRead));
                    bytesRead += r;
                }
            }
            var duration = sw.End("ReadTime");
            double totalSeconds = duration.TotalSeconds;
            double totalBytes = bytesRead;
            double bytesPerSecond = totalBytes / totalSeconds;
            return new ByteSize(Convert.ToInt64(bytesPerSecond));
        }
    }
}
