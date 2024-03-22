using CodexPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class TwoClientTests : CodexDistTest
    {
        [Test]
        public void TwoClientTest()
        {
            var uploader = AddCodex(s => s.WithName("Uploader"));
            var downloader = AddCodex(s => s.WithName("Downloader").WithBootstrapNode(uploader));

            PerformTwoClientTest(uploader, downloader);
        }

        [Test]
        public void TwoClientsTwoLocationsTest()
        {
            var locations = Ci.GetKnownLocations();
            if (locations.NumberOfLocations < 2)
            {
                Assert.Inconclusive("Two-locations test requires 2 nodes to be available in the cluster.");
                return;
            }

            var uploader = Ci.StartCodexNode(s => s.WithName("Uploader").At(locations.Get(0)));
            var downloader = Ci.StartCodexNode(s => s.WithName("Downloader").WithBootstrapNode(uploader).At(locations.Get(1)));

            PerformTwoClientTest(uploader, downloader);
        }

        private void PerformTwoClientTest(ICodexNode uploader, ICodexNode downloader)
        {
            PerformTwoClientTest(uploader, downloader, 10.MB());
        }

        private void PerformTwoClientTest(ICodexNode uploader, ICodexNode downloader, ByteSize size)
        {
            var testFile = GenerateTestFile(size);

            var contentId = uploader.UploadFile(testFile);

            var downloadedFile = downloader.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
            CheckLogForErrors(uploader, downloader);
        }
    }
}
