using DistTestCore;
using NUnit.Framework;

namespace Tests.DownloadConnectivityTests
{
    [TestFixture]
    public class FullyConnectedDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        public void FullyConnectedDownloadTest(
            [Values(1, 3, 5)] int numberOfNodes,
            [Values(1, 10)] int sizeMBs)
        {
            for (var i = 0; i < numberOfNodes; i++) SetupCodexNode();

            CreatePeerDownloadTestHelpers().AssertFullDownloadInterconnectivity(GetAllOnlineCodexNodes(), sizeMBs.MB());
        }
    }
}
