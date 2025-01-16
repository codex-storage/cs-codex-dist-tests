using CodexClient;
using FileUtils;
using Logging;
using NUnit.Framework;
using Utils;

namespace CodexTests.DownloadConnectivityTests
{
    [TestFixture]
    public class MultiswarmTests : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        public void Multiswarm(
            [Values(3, 5)] int numFiles,
            [Values(5, 20)] int fileSizeMb,
            [Values(1)] int uploadersPerFile,
            [Values(3)] int downloadersPerFile,
            [Values(1)] int maxUploadsPerNode,
            [Values(2, 3)] int maxDownloadsPerNode
        )
        {
            var plan = CreateThePlan(numFiles, uploadersPerFile, downloadersPerFile, maxUploadsPerNode, maxDownloadsPerNode);
            Assert.That(plan.NodePlans.Count, Is.LessThan(30));

            RunThePlan(plan, fileSizeMb);
        }

        private void RunThePlan(Plan plan, int fileSizeMb)
        {
            foreach (var filePlan in plan.FilePlans) filePlan.File = GenerateTestFile(fileSizeMb.MB());
            var nodes = StartCodex(plan.NodePlans.Count);
            for (int i = 0; i < plan.NodePlans.Count; i++) plan.NodePlans[i].Node = nodes[i];

            // Upload all files to their nodes.
            foreach (var filePlan in plan.FilePlans)
            {
                foreach (var uploader in filePlan.Uploaders)
                {
                    filePlan.Cid = uploader.Node!.UploadFile(filePlan.File!);
                }
            }

            Thread.Sleep(5000); // Everything is processed and announced.

            // Start all downloads (almost) simultaneously.
            var tasks = new List<Task>();
            foreach (var filePlan in plan.FilePlans)
            {
                foreach (var downloader in filePlan.Downloaders)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var downloadedFile = downloader.Node!.DownloadContent(filePlan.Cid!);
                        lock (filePlan.DownloadedFiles)
                        {
                            filePlan.DownloadedFiles.Add(downloadedFile);
                        }
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());

            // Assert all files are correct.
            foreach (var filePlan in plan.FilePlans)
            {
                foreach (var downloadedFile in filePlan.DownloadedFiles)
                {
                    filePlan.File!.AssertIsEqual(downloadedFile);
                }
            }
        }

        private Plan CreateThePlan(int numFiles, int uploadersPerFile, int downloadersPerFile, int maxUploadsPerNode, int maxDownloadsPerNode)
        {
            var plan = new Plan(numFiles, uploadersPerFile, downloadersPerFile, maxUploadsPerNode, maxDownloadsPerNode);
            plan.Initialize();
            plan.LogPlan(GetTestLog());
            return plan;
        }
    }

    public class FilePlan
    {
        public FilePlan(int number)
        {
            Number = number;
        }

        public int Number { get; }
        public TrackedFile? File { get; set; }
        public ContentId? Cid { get; set; }
        public List<TrackedFile?> DownloadedFiles { get; } = new List<TrackedFile?>();
        public List<NodePlan> Uploaders { get; } = new List<NodePlan>();
        public List<NodePlan> Downloaders { get; } = new List<NodePlan>();

        public override string ToString()
        {
            return $"FilePlan[{Number}] " +
                $"Uploaders:[{string.Join(",", Uploaders.Select(u => u.Number.ToString()))}] " +
                $"Downloaders:[{string.Join(",", Downloaders.Select(u => u.Number.ToString()))}]";
        }
    }

    public class NodePlan
    {
        public NodePlan(int number)
        {
            Number = number;
        }

        public int Number { get; }
        public ICodexNode? Node { get; set; }
        public List<FilePlan> Uploads { get; } = new List<FilePlan>();
        public List<FilePlan> Downloads { get; } = new List<FilePlan>();

        public bool Contains(FilePlan plan)
        {
            return Uploads.Contains(plan) || Downloads.Contains(plan);
        }

        public override string ToString()
        {
            return $"NodePlan[{Number}] " +
                $"Uploads:[{string.Join(",", Uploads.Select(u => u.Number.ToString()))}] " +
                $"Downloads:[{string.Join(",", Downloads.Select(u => u.Number.ToString()))}]";
        }
    }

    public class Plan
    {
        private readonly int numFiles;
        private readonly int uploadersPerFile;
        private readonly int downloadersPerFile;
        private readonly int maxUploadsPerNode;
        private readonly int maxDownloadsPerNode;

        public Plan(int numFiles, int uploadersPerFile, int downloadersPerFile, int maxUploadsPerNode, int maxDownloadsPerNode)
        {
            this.numFiles = numFiles;
            this.uploadersPerFile = uploadersPerFile;
            this.downloadersPerFile = downloadersPerFile;
            this.maxUploadsPerNode = maxUploadsPerNode;
            this.maxDownloadsPerNode = maxDownloadsPerNode;
        }

        public List<FilePlan> FilePlans { get; } = new List<FilePlan>();
        public List<NodePlan> NodePlans { get; } = new List<NodePlan>();

        public void Initialize()
        {
            for (int i = 0; i < numFiles; i++) FilePlans.Add(new FilePlan(i));
            foreach (var filePlan in FilePlans)
            {
                while (filePlan.Uploaders.Count < uploadersPerFile) AddUploader(filePlan);
                while (filePlan.Downloaders.Count < downloadersPerFile) AddDownloader(filePlan);
            }

            CollectionAssert.AllItemsAreUnique(FilePlans.Select(f => f.Number));
            CollectionAssert.AllItemsAreUnique(NodePlans.Select(f => f.Number));

            foreach (var filePlan in FilePlans)
            {
                Assert.That(filePlan.Uploaders.Count, Is.EqualTo(uploadersPerFile));
                Assert.That(filePlan.Downloaders.Count, Is.EqualTo(downloadersPerFile));
            }
            foreach (var nodePlan in NodePlans)
            {
                Assert.That(nodePlan.Uploads.Count, Is.LessThanOrEqualTo(maxUploadsPerNode));
                Assert.That(nodePlan.Downloads.Count, Is.LessThanOrEqualTo(maxDownloadsPerNode));
            }
        }

        public void LogPlan(ILog log)
        {
            log.Log("The plan:");
            log.Log("Input:");
            log.Log($"numFiles: {numFiles}");
            log.Log($"uploadersPerFile: {uploadersPerFile}");
            log.Log($"downloadersPerFile: {downloadersPerFile}");
            log.Log($"maxUploadsPerNode: {maxUploadsPerNode}");
            log.Log($"maxDownloadsPerNode: {maxDownloadsPerNode}");
            log.Log("Setup:");
            log.Log($"number of nodes: {NodePlans.Count}");
            foreach (var filePlan in FilePlans) log.Log(filePlan.ToString());
            foreach (var nodePlan in NodePlans) log.Log(nodePlan.ToString());
        }

        private void AddDownloader(FilePlan filePlan)
        {
            var nodePlan = GetOrCreateDownloaderNode(filePlan);
            filePlan.Downloaders.Add(nodePlan);
            nodePlan.Downloads.Add(filePlan);
        }

        private void AddUploader(FilePlan filePlan)
        {
            var nodePlan = GetOrCreateUploaderNode(filePlan);
            filePlan.Uploaders.Add(nodePlan);
            nodePlan.Uploads.Add(filePlan);
        }

        private NodePlan GetOrCreateDownloaderNode(FilePlan notIn)
        {
            var available = NodePlans.Where(n =>
                n.Downloads.Count < maxDownloadsPerNode && !n.Contains(notIn)
            ).ToArray();
            if (available.Any()) return RandomUtils.GetOneRandom(available);

            var newNodePlan = new NodePlan(NodePlans.Count);
            NodePlans.Add(newNodePlan);
            return newNodePlan;
        }

        private NodePlan GetOrCreateUploaderNode(FilePlan notIn)
        {
            var available = NodePlans.Where(n =>
                n.Uploads.Count < maxUploadsPerNode && !n.Contains(notIn)
            ).ToArray();
            if (available.Any()) return RandomUtils.GetOneRandom(available);

            var newNodePlan = new NodePlan(NodePlans.Count);
            NodePlans.Add(newNodePlan);
            return newNodePlan;
        }
    }
}
