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
            //"thatbenbierens/nim-codex:blkex-cancelpresence-2", // S don't send cancel-presence messages
            //"thatbenbierens/nim-codex:blkex-cancelpresence-1", // F ignore cancel-presence messages
            //"codexstorage/nim-codex:sha-4b5c355-dist-tests", // F unmodified

            //"thatbenbierens/nim-codex:blkex-cancelpresence-3", // F same as 1 but logging
            //"thatbenbierens/nim-codex:blkex-cancelpresence-4", // S no cancel-presence-msg, no fromCancel field
            //"thatbenbierens/nim-codex:blkex-cancelpresence-5", // F all-presence = cancel? return from handler
            //"thatbenbierens/nim-codex:blkex-cancelpresence-6", // F no cancel-presence-msg, but if any cancel send empty presence msg
            //"thatbenbierens/nim-codex:blkex-cancelpresence-7", // F same but logs outgoing empty presence message. (msg is empty structure)
            //"thatbenbierens/nim-codex:blkex-cancelpresence-8", // crashes F? eventtimelogging
            //"thatbenbierens/nim-codex:blkex-cancelpresence-9", // crashes S? eventtimelogging + no cancel-presence-msg (should be slow)

            //"thatbenbierens/nim-codex:blkex-cancelpresence-10", // F eventtimelogging (should be fast)
            //"thatbenbierens/nim-codex:blkex-cancelpresence-11", // S eventtimelogging + no cancel-presence-msg (should be slow)

            //"thatbenbierens/nim-codex:blkex-cancelpresence-12", // F upload and download event logging (should be fast)
            //"thatbenbierens/nim-codex:blkex-cancelpresence-13", // S same but with no cancel-presence-msg (should be slow)

            //"thatbenbierens/nim-codex:peerselecta-1", // F PR update (yes cancel-presence-msg)
            //"thatbenbierens/nim-codex:peerselecta-2", // S PR update (no cancel-presence-msg)

            //"thatbenbierens/nim-codex:blkex-cancelpresence-14", // F new logging
            //"thatbenbierens/nim-codex:blkex-cancelpresence-15", // S new logging

            //"thatbenbierens/nim-codex:blkex-cancelpresence-16-f", // F more logging
            //"thatbenbierens/nim-codex:blkex-cancelpresence-16-s", // S more logging

            //"thatbenbierens/nim-codex:blkex-cancelpresence-17-f", // F "tick" every 100 milliseconds
            //"thatbenbierens/nim-codex:blkex-cancelpresence-17-s", // S same but slow

            //"thatbenbierens/nim-codex:blkex-cancelpresence-18-f", // F "tick" every 10 milliseconds
            //"thatbenbierens/nim-codex:blkex-cancelpresence-18-s", // S same but slow

            //"thatbenbierens/nim-codex:blkex-cancelpresence-19-f", // F sending/sent/received logs
            //"thatbenbierens/nim-codex:blkex-cancelpresence-19-s", // S same but slow

            //"thatbenbierens/nim-codex:blkex-cancelpresence-20-f", // F sending/sent/received logs + number
            //"thatbenbierens/nim-codex:blkex-cancelpresence-20-s", // S same but slow

            //"thatbenbierens/nim-codex:blkex-cancelpresence-21-f", // F libp2p lpchannel.write logs
            //"thatbenbierens/nim-codex:blkex-cancelpresence-21-s", // S same but slow

            //"thatbenbierens/nim-codex:blkex-cancelpresence-22-f", // F chronos stream write logs
            //"thatbenbierens/nim-codex:blkex-cancelpresence-22-s", // S same but slow

            "thatbenbierens/nim-codex:blkex-cancelpresence-23-f", // F chronos stream write logs in libp2p hand-off
            "thatbenbierens/nim-codex:blkex-cancelpresence-23-s", // S same but slow

            "thatbenbierens/nim-codex:blkex-cancelpresence-25-f", // F chronos stream write logs in libp2p hand-off with ticks
            "thatbenbierens/nim-codex:blkex-cancelpresence-25-s",  // S same but slow

            "thatbenbierens/nim-codex:blkex-cancelpresence-27-f", // F chronos stream write logs in libp2p hand-off with ticks adds names
            "thatbenbierens/nim-codex:blkex-cancelpresence-27-s"  // S same but slow
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
            var path = "d:\\Dev\\cs-codex-dist-tests\\Tests\\CodexReleaseTests\\bin\\Debug\\net8.0\\CodexTestLogs\\2025-01\\09\\13-58-28Z_TwoClientTests\\";
            var file1 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-27-f]_000001_Downloader1.log");
            var file2 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-27-f]_000000_Uploader0.log");
            var file3 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-27-s]_000001_Downloader1.log");
            var file4 = Path.Combine(path, "TwoClientTest[thatbenbierens_nim-codex_blkex-cancelpresence-27-s]_000000_Uploader0.log");

            var lines = File.ReadAllLines(file3);
            var clines = new List<CodexLogLine>();
            foreach (var line in lines)
            {
                var cline = CodexLogLine.Parse(line);
                if (cline != null) clines.Add(cline);
            }

            var gaps = new List<Gap>();
            for (var i = 0; i < clines.Count; i++)
            {
                var line = clines[i];


                // todo:
//TRC 2025-01-09 13:59:14.501+00:00 chronosread                                topics="libp2p chronosstream custom" tid=1 ticks=424485 name=ChronosStream count=32669
//TRC 2025-01-09 13:59:14.501+00:00 chronosread                                topics="libp2p chronosstream custom" tid=1 ticks=600 name=ChronosStream count=32670
//TRC 2025-01-09 13:59:14.501+00:00 readOnce                                   topics="libp2p mplexchannel custom" tid=1 s=16U*uBBR7j:677fd62fe0c5bd152c675e42:677fd62ff7548faf70a27174 bytes=1 count=32671
//TRC 2025-01-09 13:59:14.501+00:00 readOnce                                   topics="libp2p mplexchannel custom" tid=1 s=16U*uBBR7j:677fd62fe0c5bd152c675e42:677fd62ff7548faf70a27174 bytes=73 count=32672
//TRC 2025-01-09 13:59:14.501+00:00 MsgReceived                                topics="codex blockexcnetworkpeer" tid=1 num=7 count=32673

                // read to received!???

                // run in cluster, same effect???
                // run native, same effect?

                if (line.Message == "MsgSending")
                {
                    // the next line is lpc-write-fast, then chronoswrite
                    if (i + 2  < clines.Count)
                    {
                        var next = clines[i + 2];
                        if (next.Message == "chronoswrite")
                        {
                            // got ya!
                            gaps.Add(new Gap(line, next));
                        }
                        else
                        {
                            var aaaa = "what is it?!";
                        }
                    }
                }
            }

            gaps = gaps.OrderByDescending(g => g.GapSpan.TotalMilliseconds).ToList();

            var iiii = 0;

        }

        public class Gap
        {
            public Gap(CodexLogLine line, CodexLogLine next)
            {
                Line = line;
                Next = next;
            }

            public CodexLogLine Line { get; }
            public CodexLogLine Next { get; }

            public TimeSpan GapSpan
            {
                get
                {
                    return Next.TimestampUtc - Line.TimestampUtc;
                }
            }

            public override string ToString()
            {
                return $"[{GapSpan.TotalMilliseconds} ms]";
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
