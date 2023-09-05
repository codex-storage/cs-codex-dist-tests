using DistTestCore.Codex;
using Logging;
using NUnit.Framework;
using static DistTestCore.Helpers.FullConnectivityHelper;

namespace DistTestCore.Helpers
{
    public class PeerFetchTestHelpers : IFullConnectivityImplementation
    {
        private readonly FullConnectivityHelper helper;
        private readonly BaseLog log;
        private readonly FileManager fileManager;
        private readonly ByteSize testFileSize;
        private readonly int expectedNumberOfBlocks;

        public PeerFetchTestHelpers(BaseLog log, FileManager fileManager)
        {
            helper = new FullConnectivityHelper(log, this);
            testFileSize = 10.MB();
            expectedNumberOfBlocks = 161;
            this.log = log;
            this.fileManager = fileManager;
        }

        public void AssertFullFetchInterconnectivity(IEnumerable<IOnlineCodexNode> nodes)
        {
            AssertFullFetchInterconnectivity(nodes.Select(n => ((OnlineCodexNode)n).CodexAccess));
        }

        public void AssertFullFetchInterconnectivity(IEnumerable<CodexAccess> nodes)
        {
            helper.AssertFullyConnected(nodes);
        }

        public string Description()
        {
            return "Fetch connectivity";
        }

        public string ValidateEntry(Entry entry, Entry[] allEntries)
        {
            return string.Empty;
        }

        public PeerConnectionState Check(Entry from, Entry to)
        {
            fileManager.PushFileSet();
            var expectedFile = GenerateTestFile(from.Node, to.Node);

            using var uploadStream = File.OpenRead(expectedFile.Filename);
            var contentId = Stopwatch.Measure(log, "Upload", () => from.Node.UploadFile(uploadStream));
            var originalFetch = from.Node.DebugFetch(contentId);
            Assert.That(Convert.ToInt32(originalFetch.numberOfBlocks), Is.EqualTo(expectedNumberOfBlocks));
            Assert.That(originalFetch.originalBytes.Replace("'NByte", ""), Is.EqualTo("10485760"));

            try
            {
                var fetchResponse = Stopwatch.Measure(log, "Fetch", () => to.Node.DebugFetch(contentId));
                AssertEqual(originalFetch, fetchResponse);
                return PeerConnectionState.Connection;
            }
            catch
            {
                return PeerConnectionState.NoConnection;
            }
            finally
            {
                fileManager.PopFileSet();
            }
        }

        private void AssertEqual(CodexDebugFetchResponse expected, CodexDebugFetchResponse actual)
        {
            Assert.That(actual.originalBytes, Is.EqualTo(expected.originalBytes));
            Assert.That(actual.blockSize, Is.EqualTo(expected.blockSize));
            Assert.That(actual.numberOfBlocks, Is.EqualTo(expected.numberOfBlocks));
            Assert.That(actual.version, Is.EqualTo(expected.version));
            Assert.That(actual.hcodec, Is.EqualTo(expected.hcodec));
            Assert.That(actual.codec, Is.EqualTo(expected.codec));
            Assert.That(actual.@protected, Is.EqualTo(expected.@protected));
            Assert.That(actual.blocks.Length, Is.EqualTo(expected.blocks.Length));
            for (var i = 0; i < expected.blocks.Length; i++)
            {
                Assert.That(actual.blocks[i].cid, Is.EqualTo(expected.blocks[i].cid));
            }
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
