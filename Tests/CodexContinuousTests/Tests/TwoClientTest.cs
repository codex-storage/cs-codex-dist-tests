using CodexPlugin;
using FileUtils;
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

        [TestMoment(t: Zero)]
        public void UploadTestFile()
        {
            var size = 80.MB();
            file = FileManager.GenerateFile(size);

            AssertBytesStoredMetric(size, Nodes[0], () =>
            {
                cid = Nodes[0].UploadFile(file);
                Assert.That(cid, Is.Not.Null);
            });
        }

        [TestMoment(t: 10)]
        public void DownloadTestFile()
        {
            var dl = Nodes[1].DownloadContent(cid!);

            file.AssertIsEqual(dl);
        }

        private void AssertBytesStoredMetric(ByteSize uploadedSize, ICodexNode node, Action action)
        {
            var lowExpected = uploadedSize.SizeInBytes;
            var highExpected = uploadedSize.SizeInBytes * 1.2;

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
    }
}
