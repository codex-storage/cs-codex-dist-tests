using CodexPlugin;
using CodexTests;
using DistTestCore;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Utils;

namespace CodexLongTests.BasicTests
{
    [TestFixture]
    public class LargeFileTests : CodexDistTest
    {
        #region Abort test run after first failure

        private bool stop;

        [SetUp]
        public void SetUp()
        {
            if (stop)
            {
                Assert.Inconclusive("Previous test failed");
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                stop = true;
            }
        }

        #endregion

        [TestCase(      1 *     1)] // 1 MB
        [TestCase(      1 *    10)]
        [TestCase(      1 *   100)]
        [TestCase(      1 *  1024)] // 1 GB
        [TestCase(   1024 *    10)]
        [TestCase(   1024 *   100)]
        [TestCase(   1024 *  1024)] // 1 TB :O
        [UseLongTimeouts]
        public void DownloadCorrectnessTest(long size)
        {
            var sizeMB = size.MB();

            var expectedFile = GenerateTestFile(sizeMB);

            var node = AddCodex(s => s.WithStorageQuota((size + 10).MB()));

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
