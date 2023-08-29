using DistTestCore;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class HoldMyBeerTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 1;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(5);
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
            Assert.That(numberOfBlocks, Is.EqualTo(1282));

            file = FileManager.GenerateTestFile(filesize);

            cid = UploadFile(Nodes[0], file);
            Assert.That(cid, Is.Not.Null);

            var dl = DownloadFile(Nodes[0], cid!);
            file.AssertIsEqual(dl);
        }
    }
}
