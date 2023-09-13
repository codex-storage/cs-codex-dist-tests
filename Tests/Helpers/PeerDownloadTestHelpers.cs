using CodexPlugin;
using FileUtils;
using Logging;
using Utils;
using static DistTestCore.Helpers.FullConnectivityHelper;

namespace DistTestCore.Helpers
{
    public class PeerDownloadTestHelpers : IFullConnectivityImplementation
    {
        private readonly FullConnectivityHelper helper;
        private readonly ILog log;
        private readonly IFileManager fileManager;
        private ByteSize testFileSize;

        public PeerDownloadTestHelpers(ILog log, IFileManager fileManager)
        {
            helper = new FullConnectivityHelper(log, this);
            testFileSize = 1.MB();
            this.log = log;
            this.fileManager = fileManager;
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
            return fileManager.ScopedFiles(() => CheckConnectivity(from, to));
        }

        private PeerConnectionState CheckConnectivity(Entry from, Entry to)
        {
            var expectedFile = GenerateTestFile(from.Node, to.Node);
            var contentId = Stopwatch.Measure(log, "Upload", () => from.Node.UploadFile(expectedFile));

            try
            {
                var downloadedFile = Stopwatch.Measure(log, "Download", () => DownloadFile(to.Node, contentId, expectedFile.Label + "_downloaded"));
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

        private TrackedFile? DownloadFile(IOnlineCodexNode node, ContentId contentId, string label)
        {
            return node.DownloadContent(contentId, label);
        }

        private TrackedFile GenerateTestFile(IOnlineCodexNode uploader, IOnlineCodexNode downloader)
        {
            var up = uploader.GetName().Replace("<", "").Replace(">", "");
            var down = downloader.GetName().Replace("<", "").Replace(">", "");
            var label = $"~from:{up}-to:{down}~";
            return fileManager.GenerateFile(testFileSize, label);
        }
    }
}
