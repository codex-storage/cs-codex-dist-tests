using DistTestCore;
using Logging;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class HoldMyBeerTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 1;
        public override TimeSpan RunTestEvery => TimeSpan.FromSeconds(30);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;

        private ContentId? cid;
        private TestFile file = null!;

        [TestMoment(t: Zero)]
        public void UploadTestFile()
        {
            var metadata = Configuration.CodexDeployment.Metadata;
            var maxQuotaUseMb = metadata.StorageQuotaMB / 2;
            var safeTTL = Math.Max(metadata.BlockTTL, metadata.BlockMI) + 30;
            var runsPerTtl = Convert.ToInt32(safeTTL / RunTestEvery.TotalSeconds);
            var filesizePerUploadMb = Math.Min(80, maxQuotaUseMb / runsPerTtl);
            // This filesize should keep the quota below 50% of the node's max.

            var filesize = filesizePerUploadMb.MB();
            double codexDefaultBlockSize = 31 * 64 * 33;
            var numberOfBlocks = Convert.ToInt64(Math.Ceiling(filesize.SizeInBytes / codexDefaultBlockSize));
            var sizeInBytes = filesize.SizeInBytes;
            Assert.That(numberOfBlocks, Is.EqualTo(1282));

            file = FileManager.GenerateTestFile(filesize);

            cid = UploadFile(Nodes[0], file);
            Assert.That(cid, Is.Not.Null);

            var cidTag = cid!.Id.Substring(cid.Id.Length - 6);
            Stopwatch.Measure(Log, "upload-log-asserts", () =>
            {
                var uploadLog = DownloadContainerLog(Nodes[0].Container, 50000);

                var storeLines = uploadLog.FindLinesThatContain("Stored data", "topics=\"codex node\"");
                uploadLog.DeleteFile();

                var storeLine = GetLineForCidTag(storeLines, cidTag);
                AssertStoreLineContains(storeLine, numberOfBlocks, sizeInBytes);
            });

            var dl = DownloadFile(Nodes[0], cid!);
            file.AssertIsEqual(dl);

            Stopwatch.Measure(Log, "download-log-asserts", () =>
            {
                var downloadLog = DownloadContainerLog(Nodes[0].Container, 50000);

                var sentLines = downloadLog.FindLinesThatContain("Sent bytes", "topics=\"codex restapi\"");
                downloadLog.DeleteFile();

                var sentLine = GetLineForCidTag(sentLines, cidTag);
                AssertSentLineContains(sentLine, sizeInBytes);
            });
        }

        private void AssertSentLineContains(string sentLine, long sizeInBytes)
        {
            var tag = "bytes=";
            var token = sentLine.Substring(sentLine.IndexOf(tag) + tag.Length);
            var bytes = Convert.ToInt64(token);
            Assert.AreEqual(sizeInBytes, bytes, $"Sent bytes: Number of bytes incorrect. Line: '{sentLine}'");
        }

        private void AssertStoreLineContains(string storeLine, long numberOfBlocks, long sizeInBytes)
        {
            var tokens = storeLine.Split(" ");

            var blocksToken = GetToken(tokens, "blocks=");
            var sizeToken = GetToken(tokens, "size=");
            if (blocksToken == null) Assert.Fail("blockToken not found in " + storeLine);
            if (sizeToken == null) Assert.Fail("sizeToken not found in " + storeLine);

            var blocks = Convert.ToInt64(blocksToken);
            var size = Convert.ToInt64(sizeToken?.Replace("'NByte", ""));

            var lineLog = $" Line: '{storeLine}'";
            Assert.AreEqual(numberOfBlocks, blocks, "Stored data: Number of blocks incorrect." + lineLog);
            Assert.AreEqual(sizeInBytes, size, "Stored data: Number of blocks incorrect." + lineLog);
        }

        private string GetLineForCidTag(string[] lines, string cidTag)
        {
            var result = lines.SingleOrDefault(l => l.Contains(cidTag));
            if (result == null)
            {
                Assert.Fail($"Failed to find '{cidTag}' in lines: '{string.Join(",", lines)}'");
                throw new Exception();
            }

            return result;
        }

        private string? GetToken(string[] tokens, string tag)
        {
            var token = tokens.SingleOrDefault(t => t.StartsWith(tag));
            if (token == null) return null;
            return token.Substring(tag.Length);
        }
    }
}
