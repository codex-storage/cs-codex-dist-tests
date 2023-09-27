using CodexPlugin;
using FileUtils;
using Logging;
using NUnit.Framework;
using Utils;

namespace ContinuousTests.Tests
{
    public class TwoClientTest : ContinuousTest
    {
        private const string BytesStoredMetric = "codexRepostoreBytesUsed";

        public override int RequiredNumberOfNodes => 2;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(2);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;

        private ContentId? cid;
        private TrackedFile file = null!;
        private readonly ByteSize size = 80.MB();

        [TestMoment(t: Zero)]
        public void UploadTestFile()
        {
            file = FileManager.GenerateFile(size);

            LogStoredBytes(Nodes[0]);

            LogBytesPerMillisecond(() => cid = Nodes[0].UploadFile(file));
            Assert.That(cid, Is.Not.Null);
        }

        [TestMoment(t: 10)]
        public void DownloadTestFile()
        {
            TrackedFile? dl = null;

            LogBytesPerMillisecond(() => dl = Nodes[1].DownloadContent(cid!));

            file.AssertIsEqual(dl);
        }

        private void LogStoredBytes(ICodexNode node)
        {
            var metrics = CreateMetricsAccess(node);
            var metric = metrics.GetMetric(BytesStoredMetric);
            if (metric == null)
            {
                Log.Log($"Unabled to fetch metric '{BytesStoredMetric}' for node '{node.GetName()}'");
                return;
            }

            var bytes = new ByteSize(Convert.ToInt64(metric.Values.Single().Value));

            Log.Log($"{node.GetName()}: {bytes}");
        }

        private void LogBytesPerMillisecond(Action action)
        {
            var sw = Stopwatch.Begin(Log);
            action();
            var duration = sw.End();
            double totalMs = duration.TotalMilliseconds;
            double totalBytes = size.SizeInBytes;

            var bytesPerMs = totalBytes / totalMs;
            Log.Log($"Bytes per millisecond: {bytesPerMs}");
        }
    }
}
