using CodexTests;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class ManifestOnlyDownloadTest : CodexDistTest
    {
        [Test]
        public void ManifestOnlyTest()
        {
            var uploader = StartCodex();
            var downloader = StartCodex(s => s.WithBootstrapNode(uploader));

            var file = GenerateTestFile(2.GB());
            var size = file.GetFilesize().SizeInBytes;
            var cid = uploader.UploadFile(file);

            var startSpace = downloader.Space();
            var localDataset = downloader.DownloadManifestOnly(cid);

            Thread.Sleep(1000);

            var spaceDiff = startSpace.FreeBytes - downloader.Space().FreeBytes;

            Assert.That(spaceDiff, Is.LessThan(64.KB().SizeInBytes));

            Assert.That(localDataset.Cid, Is.EqualTo(cid));
            Assert.That(localDataset.Manifest.DatasetSize.SizeInBytes, Is.EqualTo(file.GetFilesize().SizeInBytes));
        }
    }
}
