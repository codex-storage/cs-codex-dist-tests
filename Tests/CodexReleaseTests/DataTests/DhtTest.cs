using CodexClient;
using CodexTests;
using Logging;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests.DHT
{
    [Ignore("work in progress")]
    [TestFixture(10, 1000)]
    [TestFixture(50, 1000)]
    [TestFixture(10, 10000)]
    [TestFixture(20, 10000)]
    [TestFixture(30, 10000)]
    [TestFixture(50, 10000)]
    public class DhtTest : AutoBootstrapDistTest
    {
        public DhtTest(int nodes, int files)
        {
            numNodes = nodes;
            numFilesPerNode = files;
        }

        private readonly int numNodes;
        private readonly int numFilesPerNode;
        private readonly int numToFetch = 200;
        private readonly ByteSize fileSize = 100.KB();
        private readonly TimeSpan maxDownloadDuration = TimeSpan.FromSeconds(5.0);

        [Test]
        public void PressureTest()
        {
            var cids = new List<ContentId>();
            var nodes = StartCodex(numNodes);

            for (var i = 0; i < numFilesPerNode; i++)
            {
                foreach (var n in nodes)
                {
                    cids.Add(n.UploadFile(GenerateTestFile(fileSize)));
                }
            }

            // We announce both manifest-cid and tree-cid for each file;
            var estimate = numNodes * numFilesPerNode * 2;
            Log($"Estimate of DHT records: {estimate}");

            var node = StartCodex();
            for (var i = 0; i < numToFetch; i++)
            {
                var timing = Stopwatch.Measure(GetTestLog(), nameof(PressureTest) + i, () =>
                {
                    node.DownloadContent(cids.PickOneRandom());
                });

                Assert.That(timing, Is.LessThan(maxDownloadDuration));
            }
        }
    }
}
