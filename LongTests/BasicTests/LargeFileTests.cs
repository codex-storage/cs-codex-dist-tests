using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace TestsLong.BasicTests
{
    [TestFixture]
    public class LargeFileTests : DistTest
    {
        [Test]
        [Combinatorial]
        [UseLongTimeouts]
        public void DownloadCorrectnessTest(
            [Values(1, 10, 100, 1024)] int sizeInMB,
            [Values(1, 10, 100, 1024)] int multiplier)
        {
            long size = (sizeInMB * multiplier);
            var sizeMB = size.MB();

            var expectedFile = GenerateTestFile(sizeMB);

            var node = SetupCodexNode(s => s.WithStorageQuota((size + 10).MB()));

            var uploadStart = DateTime.UtcNow;
            var cid = node.UploadFile(expectedFile);
            var downloadStart = DateTime.UtcNow;
            var actualFile = node.DownloadContent(cid);
            var downloadFinished = DateTime.UtcNow;

            expectedFile.AssertIsEqual(actualFile);
            AssertTimeConstraint(uploadStart, downloadStart, downloadFinished, size);
        }

        private void AssertTimeConstraint(DateTime uploadStart, DateTime downloadStart, DateTime downloadFinished, long size)
        {
            float sizeInMB = size;
            var uploadTimePerMB = (uploadStart - downloadStart) / sizeInMB;
            var downloadTimePerMB = (downloadStart - downloadFinished) / sizeInMB;

            Assert.That(uploadTimePerMB, Is.LessThan(CodexContainerRecipe.MaxUploadTimePerMegabyte),
                "MaxUploadTimePerMegabyte performance threshold breached.");

            Assert.That(downloadTimePerMB, Is.LessThan(CodexContainerRecipe.MaxDownloadTimePerMegabyte),
                "MaxDownloadTimePerMegabyte performance threshold breached.");
        }
    }
}
