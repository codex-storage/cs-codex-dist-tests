using CodexClient;
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
        [Test]
        [Combinatorial]
        public void SmallSwarm(
            [Values(2)] int numberOfNodes,
            [Values(10)] int filesizeMb
        )
        {
            var filesize = filesizeMb.MB();
            var nodes = StartCodex(numberOfNodes);
            var files = nodes.Select(n => UploadUniqueFilePerNode(n, filesize)).ToArray();

            var tasks = ParallelDownloadEachFile(nodes, files);
            Task.WaitAll(tasks);

            AssertAllFilesDownloadedCorrectly(files);
        }

        [Test]
        [Combinatorial]
        public void StreamlessSmallSwarm(
            [Values(2)] int numberOfNodes,
            [Values(10)] int filesizeMb
        )
        {
            var filesize = filesizeMb.MB();
            var nodes = StartCodex(numberOfNodes);
            var files = nodes.Select(n => UploadUniqueFilePerNode(n, filesize)).ToArray();

            var tasks = ParallelStreamlessDownloadEachFile(nodes, files);
            Task.WaitAll(tasks);

            AssertAllFilesStreamlesslyDownloadedCorrectly(nodes, files);
        }

        private SwarmTestNetworkFile UploadUniqueFilePerNode(ICodexNode node, ByteSize fileSize)
        {
            var file = GenerateTestFile(fileSize);
            var cid = node.UploadFile(file);
            return new SwarmTestNetworkFile(node, fileSize, file, cid);
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

        private Task[] ParallelStreamlessDownloadEachFile(ICodexNodeGroup nodes, SwarmTestNetworkFile[] files)
        {
            var tasks = new List<Task>();

            foreach (var node in nodes)
            {
                tasks.Add(StartStreamlessDownload(node, files));
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

        private Task StartStreamlessDownload(ICodexNode node, SwarmTestNetworkFile[] files)
        {
            return Task.Run(() =>
            {
                var remaining = files.ToList();

                while (remaining.Count > 0)
                {
                    var file = remaining.PickOneRandom();
                    if (file.Uploader.GetName() != node.GetName())
                    {
                        try
                        {
                            var startSpace = node.Space();
                            node.DownloadStreamlessWait(file.Cid, file.OriginalSize);
                        }
                        catch (Exception ex)
                        {
                            file.Error = ex;
                        }
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

        private void AssertAllFilesStreamlesslyDownloadedCorrectly(ICodexNodeGroup nodes, SwarmTestNetworkFile[] files)
        {
            var totalFilesSpace = 0.Bytes();
            foreach (var file in files)
            {
                if (file.Error != null) throw file.Error;
                totalFilesSpace = new ByteSize(totalFilesSpace.SizeInBytes + file.Original.GetFilesize().SizeInBytes);
            }
            
            foreach (var node in nodes)
            {
                var currentSpace = node.Space();
                Assert.That(currentSpace.QuotaUsedBytes, Is.GreaterThanOrEqualTo(totalFilesSpace.SizeInBytes));
            }
        }

        private class SwarmTestNetworkFile
        {
            public SwarmTestNetworkFile(ICodexNode uploader, ByteSize originalSize, TrackedFile original, ContentId cid)
            {
                Uploader = uploader;
                OriginalSize = originalSize;
                Original = original;
                Cid = cid;
            }

            public ICodexNode Uploader { get; }
            public ByteSize OriginalSize { get; }
            public TrackedFile Original { get; }
            public ContentId Cid { get; }
            public object Lock { get; } = new object();
            public List<TrackedFile?> Downloaded { get; } = new List<TrackedFile?>();
            public Exception? Error { get; set; } = null;
        }
    }
}
