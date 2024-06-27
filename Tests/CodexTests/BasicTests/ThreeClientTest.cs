using CodexPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class ThreeClientTest : AutoBootstrapDistTest
    {
        [Test]
        public void ThreeClient()
        {
            var primary = StartCodex();
            var secondary = StartCodex();

            var testFile = GenerateTestFile(10.MB());

            var contentId = primary.UploadFile(testFile);

            var downloadedFile = secondary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }

        [Test]
        public void DownloadingUnknownCidDoesNotCauseCrash()
        {
            var node = StartCodex(2).First();

            var unknownCid = new ContentId("zDvZRwzkzHsok3Z8yMoiXE9EDBFwgr8WygB8s4ddcLzzSwwXAxLZ");

            try
            {
                node.DownloadContent(unknownCid);
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Retry 'DownloadFile' timed out"))
                {
                    throw;
                }
            }

            // Check that the node stays alive for at least another 5 minutes.
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < TimeSpan.FromMinutes(5))
            {
                Thread.Sleep(5000);
                var info = node.GetDebugInfo();
                Assert.That(!string.IsNullOrEmpty(info.Id));
            }
        }
    }
}
