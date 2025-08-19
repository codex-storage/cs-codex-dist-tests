using CodexClient;
using CodexPlugin;
using CodexTests;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class TheseusTest : AutoBootstrapDistTest
    {
        private readonly List<ICodexNode> nodes = new List<ICodexNode>();
        private TrackedFile file = null!;
        private ContentId cid = new ContentId();

        [SetUp]
        public void Setup()
        {
            file = GenerateTestFile(10.MB());
        }

        [Test]
        [Combinatorial]
        public void Theseus(
            [Values(1, 2)] int remainingNodes,
            [Values(5)] int steps)
        {
            Assert.That(remainingNodes, Is.GreaterThan(0));
            Assert.That(steps, Is.GreaterThan(remainingNodes + 1));

            nodes.AddRange(StartCodex(remainingNodes + 1));
            cid = nodes.First().UploadFile(file);

            AllNodesHaveFile();

            for (var i = 0; i < steps; i++)
            {
                Log($"{nameof(Theseus)} step {i}");
                nodes[0].Stop(waitTillStopped: true);
                nodes.RemoveAt(0);

                nodes.Add(StartCodex());

                AllNodesHaveFile();
            }
        }

        [Test]
        [Combinatorial]
        public void BlackHoleTest(
            [Values(5)]  int numBlackHoles,
            [Values(50, 200)] int sourceNodes)
        {
            var blackHoles = StartCodex(numBlackHoles, n => n.WithName("BlackHole"));

            var remaining = sourceNodes;
            var listLock = new object();
            var nodes = new List<ICodexNode>();
            var startTask = Task.Run(() =>
            {
                while (remaining > 0)
                {
                    if (nodes.Count < 3)
                    {
                        var count = Math.Min(5, remaining);
                        remaining -= count;
                        var n = StartCodex(count);
                        lock (listLock)
                        {
                            nodes.AddRange(n);
                        }
                    }
                    Thread.Sleep(100);
                }
            });

            while (remaining > 0 || nodes.Count > 0)
            {
                var node = TakeNode(nodes, listLock);
                
                var file = GenerateTestFile(200.MB());
                var cid = node.UploadFile(file);

                SimultaneousDownload(blackHoles, file, cid);

                node.Stop(waitTillStopped: false);

                AllOk(blackHoles);
            }

            startTask.Wait();
        }

        private void AllOk(ICodexNodeGroup blackHoles)
        {
            foreach (var n in blackHoles)
            {
                Assert.That(n.HasCrashed(), Is.False);
                var info = n.GetDebugInfo();
                Assert.That(string.IsNullOrEmpty(info.Spr), Is.False);
            }
        }

        private void SimultaneousDownload(ICodexNodeGroup blackHoles, TrackedFile file, ContentId cid)
        {
            var tasks = blackHoles.Select(n =>
                Task<TrackedFile>.Run(() => n.DownloadContent(cid))
            ).ToArray();

            Task.WaitAll(tasks);

            var received = tasks.Select(t => t.Result).ToArray();  

            foreach (var r in received)
            {
                file.AssertIsEqual(r);
            }
        }

        private ICodexNode TakeNode(List<ICodexNode> nodes, object listLock)
        {
            while (nodes.Count == 0)
            {
                Thread.Sleep(1000);
            }

            lock (listLock)
            {
                var n = nodes[0];
                nodes.RemoveAt(0);
                return n;
            }
        }

        private void AllNodesHaveFile()
        {
            Log($"{nameof(AllNodesHaveFile)} {nodes.Names()}");
            foreach (var n in nodes) HasFile(n);
        }

        private void HasFile(ICodexNode n)
        {
            var downloaded = n.DownloadContent(cid);
            file.AssertIsEqual(downloaded);
        }
    }
}
