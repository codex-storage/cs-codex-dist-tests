using CodexTests;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class StreamlessDownloadTest : CodexDistTest
    {
        [Test]
        public void StreamlessTest()
        {
            var uploader = StartCodex();
            var downloader = StartCodex(s => s.WithBootstrapNode(uploader));

            var file = GenerateTestFile(10.MB());
            var size = Convert.ToInt64(file.GetFilesize());
            var cid = uploader.UploadFile(file);

            var startSpace = downloader.Space();
            var start = DateTime.UtcNow;
            var localDataset = downloader.DownloadStreamless(cid);

            Assert.That(localDataset.Cid, Is.EqualTo(cid));
            Assert.That(localDataset.Manifest.OriginalBytes.SizeInBytes, Is.EqualTo(file.GetFilesize().SizeInBytes));

            // TODO: We have no way to inspect the status or progress of the download.
            // We use local space information to estimate.
            var retry = new Retry("Checking local space",
                maxTimeout: TimeSpan.FromMinutes(2),
                sleepAfterFail: TimeSpan.FromSeconds(3),
                onFail: f => { });

            retry.Run(() =>
            {
                var space = downloader.Space();
                Assert.That(space.FreeBytes, Is.LessThanOrEqualTo(startSpace.FreeBytes - size));
            });

            // Stop the uploader node and verify that the downloader has the data.
            uploader.Stop(waitTillStopped: true);
            var downloaded = downloader.DownloadContent(cid);
            file.AssertIsEqual(downloaded);
        }
    }
}
