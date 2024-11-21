using CodexPlugin;
using CodexTests;
using FileUtils;
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
    public class SwarmTests : AutoBootstrapDistTest
    {
        private const int NumberOfNodes = 5;
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
                foreach (var file in files)
                {
                    tasks.Add(StartDownload(node, file));
                }
            }

            return tasks.ToArray();
        }

        private Task StartDownload(ICodexNode node, SwarmTestNetworkFile file)
        {
            return Task.Run(() =>
            {
                try
                {
                    file.Downloaded = node.DownloadContent(file.Cid);
                }
                catch (Exception ex)
                {
                    file.Error = ex;
                }
            });
        }

        private void AssertAllFilesDownloadedCorrectly(SwarmTestNetworkFile[] files)
        {
            foreach (var file in files)
            {
                if (file.Error != null) throw file.Error;
                file.Original.AssertIsEqual(file.Downloaded);
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
            public TrackedFile? Downloaded { get; set; }
            public Exception? Error { get; set; } = null;
        }
    }
}
