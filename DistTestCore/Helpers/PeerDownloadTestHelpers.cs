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
            foreach (var node in nodes)
            {
                var uploader = node;
                var downloaders = nodes.Where(n => n != uploader).ToArray();

                test.ScopedTestFiles(() =>
                {
                    PerformTest(uploader, downloaders);
                });
            }
        }

        private void PerformTest(IOnlineCodexNode uploader, IOnlineCodexNode[] downloaders)
        {
            // 1 test file per downloader.
            var files = downloaders.Select(d => test.GenerateTestFile(1.MB())).ToArray();

            // Upload all the test files to the uploader.
            var contentIds = files.Select(uploader.UploadFile).ToArray();

            // Each downloader should retrieve its own test file.
            for (var i = 0; i < downloaders.Length; i++)
            {
                var expectedFile = files[i];
                var downloadedFile = downloaders[i].DownloadContent(contentIds[i]);

                expectedFile.AssertIsEqual(downloadedFile);
            }
        }
    }
}
