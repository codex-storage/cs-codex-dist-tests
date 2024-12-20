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
        [Combinatorial]
        public void TwoClientTest(
            [Values(
            "thatbenbierens/nim-codex:blkex-cancelpresence-2", // S don't send cancel-presence messages
            "thatbenbierens/nim-codex:blkex-cancelpresence-1", // F ignore cancel-presence messages
            "codexstorage/nim-codex:sha-4b5c355-dist-tests", // F unmodified

            "thatbenbierens/nim-codex:blkex-cancelpresence-3", // F same as 1 but logging
            "thatbenbierens/nim-codex:blkex-cancelpresence-4", // S no cancel-presence-msg, no fromCancel field
            "thatbenbierens/nim-codex:blkex-cancelpresence-5", // F all-presence = cancel? return from handler
            "thatbenbierens/nim-codex:blkex-cancelpresence-6", // F no cancel-presence-msg, but if any cancel send empty presence msg
            "thatbenbierens/nim-codex:blkex-cancelpresence-7", // F same but logs outgoing empty presence message. (msg is empty structure)
            "thatbenbierens/nim-codex:blkex-cancelpresence-8", // crashes F? eventtimelogging
            "thatbenbierens/nim-codex:blkex-cancelpresence-9", // crashes S? eventtimelogging + no cancel-presence-msg (should be slow)

            "thatbenbierens/nim-codex:blkex-cancelpresence-10", // F eventtimelogging (should be fast)
            "thatbenbierens/nim-codex:blkex-cancelpresence-11", // S eventtimelogging + no cancel-presence-msg (should be slow)

            "thatbenbierens/nim-codex:blkex-cancelpresence-12", // F upload and download event logging (should be fast)
            "thatbenbierens/nim-codex:blkex-cancelpresence-13", // S same but with no cancel-presence-msg (should be slow)

            "thatbenbierens/nim-codex:peerselecta-1", // F PR update (yes cancel-presence-msg)
            "thatbenbierens/nim-codex:peerselecta-2", // S PR update (no cancel-presence-msg)

            "thatbenbierens/nim-codex:blkex-cancelpresence-14", // F new logging
            "thatbenbierens/nim-codex:blkex-cancelpresence-15" // S new logging

            )] string img
        )
        {
            CodexContainerRecipe.DockerImageOverride = img;

            var uploader = StartCodex(s => s.WithName("Uploader"));
            var downloader = StartCodex(s => s.WithName("Downloader").WithBootstrapNode(uploader));

            PerformTwoClientTest(uploader, downloader);
        }

        [Test]
        public void ParseLogs()
        {
            var path = "";
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var cline = CodexLogLine.Parse(line);
                if (cline == null) continue;

                if (cline.Message == "times for")
                {
                    ProcessTimes(cline);
                }
            }
        }

        private void ProcessTimes(CodexLogLine cline)
        {
            // reqCreatedTime
            // wantHaveSentTimes
            // presenceRecvTimes
            // wantBlkSentTimes
            // blkRecvTimes
            // cancelSentTimes
            // resolveTimes

        }

        public class BlockReqTimes
        {
            public TimeSpan CreateToWantHaveSent { get; set; }

        }

        private void PerformTwoClientTest(ICodexNode uploader, ICodexNode downloader)
        {
            PerformTwoClientTest(uploader, downloader, 100.MB());
        }

        private void PerformTwoClientTest(ICodexNode uploader, ICodexNode downloader, ByteSize size)
        {
            var testFile = GenerateTestFile(size);

            var contentId = uploader.UploadFile(testFile);
            AssertNodesContainFile(contentId, uploader);

            var (downloadedFile, timeTaken) = downloader.DownloadContentT(contentId);
            AssertNodesContainFile(contentId, uploader, downloader);

            Assert.That(timeTaken, Is.LessThan(TimeSpan.FromSeconds(15.0)), "Too slow!");

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
