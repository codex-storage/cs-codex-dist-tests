using CodexClient;
using CodexTests;
using NUnit.Framework;
using Utils;

namespace ExperimentalTests.DownloadConnectivityTests
{
    /// <summary>
    /// https://hackmd.io/rwPtPJ7KTw6cGjhN0zNYig
    /// </summary>
    [TestFixture]
    public class DatalayerReliabilityTests : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        public void SingleSetTest(
            [Values(10, 100, 1000)] int fileSizeMb,
            [Values(5, 10, 20, 30)] int numDownloaders
        )
        {
            var file = GenerateTestFile(fileSizeMb.MB());
            var uploader = StartCodex(n => n.WithName("uploader"));
            var downloaders = StartCodex(numDownloaders, n => n.WithName("downloader"));

            var cid = uploader.UploadFile(file);

            var downloadTasks = new List<Task>();
            foreach (var dl in downloaders)
            {
                downloadTasks.Add(Task.Run(() =>
                {
                    dl.DownloadContent(cid);
                }));
            }

            Task.WaitAll(downloadTasks.ToArray());

            Assert.That(downloadTasks.All(t => !t.IsFaulted));
        }

        public class TransferPlan
        {
            public ICodexNode Uploader { get; set; } = null!;
            public ContentId Cid { get; set; } = null!;
            public List<DownloaderPlan> Downloaders { get; } = new List<DownloaderPlan>();
        }

        public class DownloaderPlan
        {
            public ICodexNode Node { get; set; } = null!;
            public List<TransferPlan> TransferPlans { get; } = new List<TransferPlan>();
        }

        public class AvailableDownloaders
        {
            private readonly List<DownloaderPlan> all = new List<DownloaderPlan>();
            private readonly List<DownloaderPlan> available = new List<DownloaderPlan>();
            private readonly int maxUsagePerDownloader;
            private readonly int numDownloadersPerPlan;

            public AvailableDownloaders(int maxUsagePerDownloader, int numDownloadersPerPlan)
            {
                this.maxUsagePerDownloader = maxUsagePerDownloader;
                this.numDownloadersPerPlan = numDownloadersPerPlan;
            }

            public void Assign(TransferPlan plan)
            {
                while (plan.Downloaders.Count < numDownloadersPerPlan)
                {
                    var open = available.Where(a => !plan.Downloaders.Contains(a)).ToArray();
                    if (!open.Any())
                    {
                        var dl = new DownloaderPlan();
                        all.Add(dl);
                        available.Add(dl);
                    }
                    else
                    {
                        var dl = RandomUtils.GetOneRandom(open);
                        dl.TransferPlans.Add(plan);
                        plan.Downloaders.Add(dl);

                        if (dl.TransferPlans.Count == maxUsagePerDownloader) available.Remove(dl);
                    }
                }
            }

            public DownloaderPlan[] GetAll()
            {
                return all.ToArray();
            }
        }

        [Test]
        [Combinatorial]
        public void MultiSetTest(
            [Values(1, 3, 5, 10)] int numDatasets,
            [Values(1, 10, 100, 1000)] int fileSizeMb,
            [Values(5, 10, 20, 30)] int numDownloadersPerDataset,
            [Values(3, 5)] int maxDatasetsPerDownloader
        )
        {
            var plans = new List<TransferPlan>();
            var uploaders = StartCodex(numDatasets, n => n.WithName("uploader"));
            foreach (var n in uploaders)
            {
                plans.Add(new TransferPlan
                {
                    Uploader = n,
                    Cid = n.UploadFile(GenerateTestFile(fileSizeMb.MB()))
                });
            }
            
            Assert.That(plans.Count, Is.GreaterThan(0));

            var available = new AvailableDownloaders(maxDatasetsPerDownloader, numDownloadersPerDataset);
            foreach (var plan in plans)
            {
                available.Assign(plan);
            }
            var allDownloaderPlans = available.GetAll();
            Assert.That(allDownloaderPlans.Length, Is.LessThan(100));
            Log($"Using {allDownloaderPlans.Length} downloaders...");
            var nodes = StartCodex(allDownloaderPlans.Length, n => n.WithName("downloader"));
            for (var i = 0; i < allDownloaderPlans.Length; i++)
            {
                allDownloaderPlans[i].Node = nodes[i];
            }

            var downloadTasks = new List<Task>();
            foreach (var dlPlan in allDownloaderPlans)
            {
                downloadTasks.Add(Task.Run(() =>
                {
                    var tf = dlPlan.TransferPlans.ToList();
                    while (tf.Count > 0)
                    {
                        var t = tf.PickOneRandom();
                        dlPlan.Node.DownloadContent(t.Cid);
                    }
                }));
            }

            Task.WaitAll(downloadTasks.ToArray());

            Assert.That(downloadTasks.All(t => !t.IsFaulted));
        }
    }
}
