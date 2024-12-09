using CodexPlugin;
using CodexTests;
using FileUtils;
using Logging;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class SwarmTests : CodexDistTest
    {
        [Test]
        [Ignore("a")]
        [Combinatorial]
        public void SmallSwarm(
            [Values(true, false)] bool peerImage,
            [Values(2, 5, 10)] int NumberOfNodes,
            [Values(2, 5, 10)] int FileSizeMb
        )
        {
            // "thatbenbierens/nim-codex:peerselect2"
            // "codexstorage/nim-codex:latest-dist-tests"

            if (peerImage)
            {
                CodexContainerRecipe.DockerImageOverride = "thatbenbierens/nim-codex:peerselect2";
            }
            else
            {
                CodexContainerRecipe.DockerImageOverride = "codexstorage/nim-codex:0.1.9-dist-tests";
            }

            var boot = StartCodex(s => s.WithName("Bootstrap"));

            var nodes = StartCodex(NumberOfNodes, s => s.WithBootstrapNode(boot));
            var files = nodes.Select(n => UploadUniqueFilePerNode(n, FileSizeMb)).ToArray();

            var tasks = ParallelDownloadEachFile(nodes, files);
            Task.WaitAll(tasks);

            AssertAllFilesDownloadedCorrectly(files);
        }

        [Test]
        [Combinatorial]
        public void Spppeeeed(
            [Values(
            23
            )] int idx
        )
        {
            string[] imgs = [
                            //"thatbenbierens/nim-codex:peerselect2", // S review comments
            "thatbenbierens/nim-codex:peerselect3", // X send wrong wantblock
            "thatbenbierens/nim-codex:peerselect4", // S send wrong wantblock
            "thatbenbierens/nim-codex:peerselect5", // X old task handler
            "thatbenbierens/nim-codex:peerselect6", // S track only wantblocks
            "thatbenbierens/nim-codex:peerselect7", // S old request proc
            "thatbenbierens/nim-codex:peerselect8", // F! 3607b88 - sends wantBlock to peers with block. wantHave to everyone else
            
            "thatbenbierens/nim-codex:peerselect9", // [6] F + Slowing! 64e691b - Fixes issue where peerWants are only stored for type wantBlock.
            //[2024-12-05T15:38:32.3332175Z] DL(12 secs)
            //[2024-12-05T15:38:58.6083189Z] DL(20 secs)
            //[2024-12-05T15:39:36.2547136Z] DL(29 secs)
            //[2024-12-05T15:40:27.0061893Z] DL(38 secs)
            //[2024-12-05T15:41:27.8224210Z] DL(47 secs)

            "thatbenbierens/nim-codex:peerselect9log2", // S [7] same, var schedule + checking peerwants list length
            "thatbenbierens/nim-codex:peerselect9log7", // S but stable [8] same, var schedule + checking peerwants list length
            "thatbenbierens/nim-codex:peerselect9log8", // S [9] patch for not storing cancels + ed3e91c - Review comments by Dmitriy
            "thatbenbierens/nim-codex:peerselect9log9", // S [10] ref object
            "thatbenbierens/nim-codex:peerselect9log10", // S [11] presencecheck only new wants
            "thatbenbierens/nim-codex:peerselect9log11", // S [12] same but no metrics + trace "wantList.entries.len" == always 1
            "thatbenbierens/nim-codex:peerselect9log12", // S [13] always schedule peer
            "thatbenbierens/nim-codex:peerselect9log13", // F! [14] 64e691b + new entry add if not e.cancel
            "thatbenbierens/nim-codex:peerselect9log14", // F [15] ed3e91c + proc wantListHandler from "64e691b + new entry add if not e.cancel"
            "thatbenbierens/nim-codex:peerselect9log15", // F [16] same, move metrics up
            "thatbenbierens/nim-codex:peerselect9log16", // F [17] 1f063fe (branchlatest) proc wantlisthandler from previous
            "thatbenbierens/nim-codex:peerselect9log17", // F [18] prev + restore schedulePeer bool
            "thatbenbierens/nim-codex:peerselect9log18", // S [19] newcommit + moves presence check behind !cancel + type = wantHave
            "thatbenbierens/nim-codex:peerselect9log19", // S [20] newcommit + moves presence check behind !cancel

            "thatbenbierens/nim-codex:peerselect9log20", // F! [21] newcommit + moves presence check behind type == wanthave
            "thatbenbierens/nim-codex:peerselect9log22", // ? [22] same + logging + logging
            "thatbenbierens/nim-codex:peerselect9log23", // ? [23] same + logging + logging intentionally broken to compare!



            "codexstorage/nim-codex:0.1.9-dist-tests", // F
            "codexstorage/nim-codex:sha-8e29939-dist-tests", // F 8e29939 - Send pluralized wantBlock messages (#1016)
            "codexstorage/nim-codex:sha-2124996-dist-tests"  // F 2124996 - Requesting the same CID sometimes causes a worker to discard the request if it's already inflight by another worker. (#1002)
                ];

            var img = imgs[idx];

            CodexContainerRecipe.DockerImageOverride = img;

            var boot = StartCodex(s => s.WithName("Bootstrap"));
            var uploader = StartCodex(s => s.WithName("Uploader").WithBootstrapNode(boot));
            var downloader = StartCodex(s => s.WithName("Downloader").WithBootstrapNode(boot));

            var total = TimeSpan.Zero;
            var number = 1;

            for (var i = 0; i < number; i++)
            {
                var file = GenerateTestFile(100.MB());
                var cid = uploader.UploadFile(file);

                var duration = Stopwatch.Measure(GetTestLog(), "DL", () =>
                {
                    downloader.DownloadContent(cid);
                }) ;
                
                total += duration;
                if (duration.TotalMinutes > 1.0) Assert.Fail("too slow");
            }

            var avg = total / number;
            Log($"{img} 100MB download average duration: {avg}");
        }

        private SwarmTestNetworkFile UploadUniqueFilePerNode(ICodexNode node, int fileSizeMb)
        {
            var file = GenerateTestFile(fileSizeMb.MB());
            var cid = node.UploadFile(file);
            return new SwarmTestNetworkFile(file, cid);
        }

        private Task[] ParallelDownloadEachFile(ICodexNodeGroup nodes, SwarmTestNetworkFile[] files)
        {
            var tasks = new List<Task>();

            foreach (var node in nodes)
            {
                tasks.Add(StartDownload(node, files));
            }

            return tasks.ToArray();
        }

        private Task StartDownload(ICodexNode node, SwarmTestNetworkFile[] files)
        {
            return Task.Run(() =>
            {
                var remaining = files.ToList();

                while (remaining.Count > 0)
                {
                    var file = remaining.PickOneRandom();
                    try
                    {
                        var dl = node.DownloadContent(file.Cid);
                        lock (file.Lock)
                        {
                            file.Downloaded.Add(dl);
                        }
                    }
                    catch (Exception ex)
                    {
                        file.Error = ex;
                    }
                }
            });
        }

        private void AssertAllFilesDownloadedCorrectly(SwarmTestNetworkFile[] files)
        {
            foreach (var file in files)
            {
                if (file.Error != null) throw file.Error;
                lock (file.Lock)
                {
                    foreach (var dl in file.Downloaded)
                    {
                        file.Original.AssertIsEqual(dl);
                    }
                }
            }
        }

        private class SwarmTestNetworkFile
        {
            public SwarmTestNetworkFile(TrackedFile original, ContentId cid)
            {
                Original = original;
                Cid = cid;
            }

            public TrackedFile Original { get; }
            public ContentId Cid { get; }
            public object Lock { get; } = new object();
            public List<TrackedFile?> Downloaded { get; } = new List<TrackedFile?>();
            public Exception? Error { get; set; } = null;
        }
    }
}
