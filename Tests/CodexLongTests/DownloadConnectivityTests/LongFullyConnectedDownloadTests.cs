using CodexTests;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace CodexLongTests.DownloadConnectivityTests
{
    [TestFixture]
    public class LongFullyConnectedDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        [UseLongTimeouts]
        [Combinatorial]
        public void FullyConnectedDownloadTest(
            [Values(10, 15, 20)] int numberOfNodes,
            [Values(10, 100)] int sizeMBs)
        {
            for (var i = 0; i < numberOfNodes; i++) AddCodex();

            CreatePeerDownloadTestHelpers().AssertFullDownloadInterconnectivity(GetAllOnlineCodexNodes(), sizeMBs.MB());
        }

        public LongFullyConnectedDownloadTests(string deployId) : base(deployId)
        {
        }
    }
}
