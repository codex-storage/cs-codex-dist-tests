using Nethereum.Model;
using NUnit.Framework;

namespace DistTestCore.Helpers
{
    public class PeerDownloadTestHelpers
    {
        private readonly DistTest test;

        public PeerDownloadTestHelpers(DistTest test)
        {
            this.test = test;
        }

        public void AssertFullDownloadInterconnectivity(IEnumerable<IOnlineCodexNode> nodes)
        {
            AssertFullDownloadInterconnectivity(nodes, 1.MB());
        }

        public void AssertFullDownloadInterconnectivity(IEnumerable<IOnlineCodexNode> nodes, ByteSize testFileSize)
        {
            test.Log($"Asserting full download interconnectivity for nodes: '{string.Join(",", nodes.Select(n => n.GetName()))}'...");
            var start = DateTime.UtcNow;

            foreach (var node in nodes)
            {
                var uploader = node;
                var downloaders = nodes.Where(n => n != uploader).ToArray();

                test.ScopedTestFiles(() =>
                {
                    PerformTest(uploader, downloaders, testFileSize);
                });
            }
            
            test.Log($"Success! Full download interconnectivity for nodes: {string.Join(",", nodes.Select(n => n.GetName()))}");
            var timeTaken = DateTime.UtcNow - start;

            AssertTimePerDownload(timeTaken, nodes.Count());
        }

        private void AssertTimePerDownload(TimeSpan timeTaken, int numberOfNodes)
        {
            var numberOfDownloads = numberOfNodes * (numberOfNodes - 1);
            var timePerDownload = timeTaken / numberOfDownloads;

            test.Log($"Performed {numberOfDownloads} downloads in {timeTaken.TotalSeconds} seconds, for an average of {timePerDownload.TotalSeconds} seconds per download.");

            Assert.That(timePerDownload.TotalSeconds, Is.LessThan(20.0f), "Seconds-per-Download breached performance threshold.");
        }

        private void PerformTest(IOnlineCodexNode uploader, IOnlineCodexNode[] downloaders, ByteSize testFileSize)
        {
            // Generate 1 test file per downloader.
            var files = downloaders.Select(d => GenerateTestFile(uploader, d, testFileSize)).ToArray();

            // Upload all the test files to the uploader.
            var contentIds = files.Select(uploader.UploadFile).ToArray();

            // Each downloader should retrieve its own test file.
            for (var i = 0; i < downloaders.Length; i++)
            {
                var expectedFile = files[i];
                var downloadedFile = downloaders[i].DownloadContent(contentIds[i], $"{expectedFile.Label}DOWNLOADED");

                expectedFile.AssertIsEqual(downloadedFile);
            }
        }

        private TestFile GenerateTestFile(IOnlineCodexNode uploader, IOnlineCodexNode downloader, ByteSize testFileSize)
        {
            var up = uploader.GetName().Replace("<", "").Replace(">", "");
            var down = downloader.GetName().Replace("<", "").Replace(">", "");
            var label = $"FROM{up}TO{down}";
            return test.GenerateTestFile(testFileSize, label);
        }
    }
}
