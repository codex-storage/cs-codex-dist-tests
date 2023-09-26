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

            AssertBytesStoredMetric(Nodes[0], () =>
            {
                LogBytesPerMillisecond(() => cid = Nodes[0].UploadFile(file));
                Assert.That(cid, Is.Not.Null);
            });
        }

        [TestMoment(t: 10)]
        public void DownloadTestFile()
        {
            TrackedFile? dl = null;

            LogBytesPerMillisecond(() => dl = Nodes[1].DownloadContent(cid!));

            file.AssertIsEqual(dl);
        }

        private void AssertBytesStoredMetric(ICodexNode node, Action action)
        {
            var lowExpected = size.SizeInBytes;
            var highExpected = size.SizeInBytes * 1.2;

            var metrics = CreateMetricsAccess(node);
            var before = metrics.GetMetric(BytesStoredMetric);

            action();

            Log.Log($"Waiting for between {lowExpected} and {highExpected} new bytes to be stored by node {node.GetName()}.");

            Time.WaitUntil(() =>
            {
                var after = metrics.GetMetric(BytesStoredMetric);
                var newBytes = Convert.ToInt64(after.Values.Last().Value - before.Values.Last().Value);

                return highExpected > newBytes && newBytes > lowExpected;
            });
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
