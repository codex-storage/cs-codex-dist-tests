using NUnit.Framework;
using Utils;

namespace CodexTests.DownloadConnectivityTests
{
    [TestFixture]
    public class DetectBlockRetransmitTest : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        [CreateTranscript("swarm_retransmit")]
        public void DetectBlockRetransmits(
            [Values(1, 5, 10, 20)] int fileSize,
            [Values(3, 5, 10, 20)] int numNodes
        )
        {
            var nodes = StartCodex(numNodes);
            var file = GenerateTestFile(fileSize.MB());
            var cid = nodes[0].UploadFile(file);

            var tasks = nodes.Select(n => Task.Run(() => n.DownloadContent(cid))).ToArray();
            Task.WaitAll(tasks);
        }
    }
}
