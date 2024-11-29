using CodexPlugin;
using CodexTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class TwoClientTests : CodexDistTest
    {
        [Test]
        public void TwoClientTest()
        {
            var uploader = StartCodex(s => s.WithName("Uploader"));
            var downloader = StartCodex(s => s.WithName("Downloader").WithBootstrapNode(uploader));

            PerformTwoClientTest(uploader, downloader);
        }

        [Test]
        [Ignore("Location selection is currently unavailable.")]
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
            AssertNodesContainFile(contentId, uploader);

            var downloadedFile = downloader.DownloadContent(contentId);
            AssertNodesContainFile(contentId, uploader, downloader);

            testFile.AssertIsEqual(downloadedFile);
            CheckLogForErrors(uploader, downloader);
        }
    }
}
