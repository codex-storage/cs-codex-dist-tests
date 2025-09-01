using CodexClient;
using CodexPlugin;
using CodexTests;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    namespace SwarmTests
    {
        [TestFixture(2, 10)]
        [TestFixture(5, 20)]
        [TestFixture(10, 20)]
        public class SwarmTests : AutoBootstrapDistTest
        {
            private readonly int numberOfNodes;
            private readonly int filesizeMb;
            private ICodexNodeGroup nodes = null!;

            public SwarmTests(int numberOfNodes, int filesizeMb)
            {
                this.numberOfNodes = numberOfNodes;
                this.filesizeMb = filesizeMb;
            }

            [TearDown]
            public void TearDown()
            {
                ITransferSpeeds speeds = new TransferSpeeds();
                foreach (var n in nodes)
                {
                    speeds = speeds.Combine(n.TransferSpeeds);
                }
                Log($"Average upload speed: {speeds.GetUploadSpeed()}");
                Log($"Average download speed: {speeds.GetDownloadSpeed()}");
            }

            [Test]
            public void Stream()
            {
                var filesize = filesizeMb.MB();
                nodes = StartCodex(numberOfNodes);
                var files = nodes.Select(n => UploadUniqueFilePerNode(n, filesize)).ToArray();

                var tasks = ParallelDownloadEachFile(files);
                Task.WaitAll(tasks);

                AssertAllFilesDownloadedCorrectly(files);
            }

            [Test]
            public void Streamless()
            {
                var filesize = filesizeMb.MB();
                nodes = StartCodex(numberOfNodes);
                var files = nodes.Select(n => UploadUniqueFilePerNode(n, filesize)).ToArray();

                var tasks = ParallelStreamlessDownloadEachFile(files);
                Task.WaitAll(tasks);

                AssertAllFilesStreamlesslyDownloadedCorrectly(files);
            }

            private SwarmTestNetworkFile UploadUniqueFilePerNode(ICodexNode node, ByteSize fileSize)
            {
                var file = GenerateTestFile(fileSize);
                var cid = node.UploadFile(file);
                return new SwarmTestNetworkFile(node, fileSize, file, cid);
            }

            private Task[] ParallelDownloadEachFile(SwarmTestNetworkFile[] files)
            {
                var tasks = new List<Task>();

                foreach (var node in nodes)
                {
                    tasks.Add(StartDownload(node, files));
                }

                return tasks.ToArray();
            }

            private Task[] ParallelStreamlessDownloadEachFile(SwarmTestNetworkFile[] files)
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
                            var dl = node.DownloadContent(file.Cid, TimeSpan.FromMinutes(30));
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

            private void AssertAllFilesStreamlesslyDownloadedCorrectly(SwarmTestNetworkFile[] files)
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
}
