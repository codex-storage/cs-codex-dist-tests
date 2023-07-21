using static DistTestCore.Helpers.FullConnectivityHelper;

namespace DistTestCore.Helpers
{
    public class PeerDownloadTestHelpers : IFullConnectivityImplementation
    {
        private readonly FullConnectivityHelper helper;
        private readonly DistTest test;
        private ByteSize testFileSize;

        public PeerDownloadTestHelpers(DistTest test)
        {
            helper = new FullConnectivityHelper(test, this);
            testFileSize = 1.MB();
            this.test = test;
        }

        public void AssertFullDownloadInterconnectivity(IEnumerable<IOnlineCodexNode> nodes, ByteSize testFileSize)
        {
            this.testFileSize = testFileSize;
            helper.AssertFullyConnected(nodes);
        }

        public string Description()
        {
            return "Download Connectivity";
        }

        public string ValidateEntry(Entry entry, Entry[] allEntries)
        {
            return string.Empty;
        }

        public PeerConnectionState Check(Entry from, Entry to)
        {
            var expectedFile = GenerateTestFile(from.Node, to.Node);

            var contentId = from.Node.UploadFile(expectedFile);

            try
            {
                var downloadedFile = to.Node.DownloadContent(contentId, expectedFile.Label + "_downloaded");
                expectedFile.AssertIsEqual(downloadedFile);
                return PeerConnectionState.Connection;
            }
            catch
            {
                // Should an exception occur during the download or file-content assertion,
                // We consider that as no-connection for the purpose of this test.
                return PeerConnectionState.NoConnection;
            }

            // Should an exception occur during upload, then this try is inconclusive and we try again next loop.
        }

        private TestFile GenerateTestFile(IOnlineCodexNode uploader, IOnlineCodexNode downloader)
        {
            var up = uploader.GetName().Replace("<", "").Replace(">", "");
            var down = downloader.GetName().Replace("<", "").Replace(">", "");
            var label = $"~from:{up}-to:{down}~";
            return test.GenerateTestFile(testFileSize, label);
        }
    }
}
