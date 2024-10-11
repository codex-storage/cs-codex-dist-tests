using NUnit.Framework;
using Utils;

namespace CodexTests.DownloadConnectivityTests
{
    [TestFixture]
    public class SwarmTests : AutoBootstrapDistTest
    {
        [Test]
        [CreateTranscript("swarm_retransmit")]
        public void DetectBlockRetransmits()
        {
            var nodes = StartCodex(10);
            var file = GenerateTestFile(10.MB());
            var cid = nodes[0].UploadFile(file);

            var tasks = nodes.Select(n => Task.Run(() => n.DownloadContent(cid))).ToArray();
            Task.WaitAll(tasks);
        }
    }
}
