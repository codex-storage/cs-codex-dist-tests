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
            [Values(1, 2, 5)] int remainingNodes,
            [Values(10)] int steps)
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

        private void AllNodesHaveFile()
        {
            WaitAndCheckNodesStaysAlive(TimeSpan.FromSeconds(20), nodes.ToArray());

            Log($"{nameof(AllNodesHaveFile)} {nodes.Names()}");
            foreach (var n in nodes) HasFile(n);

            WaitAndCheckNodesStaysAlive(TimeSpan.FromSeconds(20), nodes.ToArray());
        }

        private void HasFile(ICodexNode n)
        {
            var downloaded = n.DownloadContent(cid);
            file.AssertIsEqual(downloaded);
        }
    }
}
