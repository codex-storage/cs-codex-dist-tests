using CodexPlugin;
using CodexTests;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class SwarmTests : AutoBootstrapDistTest
    {
        private const int NumberOfNodes = 2;
        private const int FileSizeMb = 2;

        [Test]
        public void SmallSwarm()
        {
            var nodes = StartCodex(NumberOfNodes);
            var files = nodes.Select(UploadUniqueFilePerNode).ToArray();

            var tasks = ParallelDownloadEachFile(nodes, files);
            Task.WaitAll(tasks);

            AssertAllFilesDownloadedCorrectly(files);
        }

        private SwarmTestNetworkFile UploadUniqueFilePerNode(ICodexNode node)
        {
            var file = GenerateTestFile(FileSizeMb.MB());
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
