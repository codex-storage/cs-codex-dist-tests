using DistTestCore.Codex;
using FileUtils;
using Logging;
using Utils;
using static DistTestCore.Helpers.FullConnectivityHelper;

namespace DistTestCore.Helpers
{
    public class PeerDownloadTestHelpers : IFullConnectivityImplementation
    {
        private readonly FullConnectivityHelper helper;
        private readonly BaseLog log;
        private readonly FileManager fileManager;
        private ByteSize testFileSize;

        public PeerDownloadTestHelpers(BaseLog log, FileManager fileManager)
        {
            helper = new FullConnectivityHelper(log, this);
            testFileSize = 1.MB();
            this.log = log;
            this.fileManager = fileManager;
        }

        public void AssertFullDownloadInterconnectivity(IEnumerable<IOnlineCodexNode> nodes, ByteSize testFileSize)
        {
            AssertFullDownloadInterconnectivity(nodes.Select(n => ((OnlineCodexNode)n).CodexAccess), testFileSize);
        }

        public void AssertFullDownloadInterconnectivity(IEnumerable<CodexAccess> nodes, ByteSize testFileSize)
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

            using var uploadStream = File.OpenRead(expectedFile.Filename);
            var contentId = Stopwatch.Measure(log, "Upload", () => from.Node.UploadFile(uploadStream));

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

        private TestFile DownloadFile(CodexAccess node, string contentId, string label)
        {
            var downloadedFile = fileManager.CreateEmptyTestFile(label);
            using var downloadStream = File.OpenWrite(downloadedFile.Filename);
            using var stream = node.DownloadFile(contentId);
            stream.CopyTo(downloadStream);
            return downloadedFile;
        }

        private TestFile GenerateTestFile(CodexAccess uploader, CodexAccess downloader)
        {
            var up = uploader.GetName().Replace("<", "").Replace(">", "");
            var down = downloader.GetName().Replace("<", "").Replace(">", "");
            var label = $"~from:{up}-to:{down}~";
            return fileManager.GenerateTestFile(testFileSize, label);
        }
    }
}
