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
            [Values(3, 10, 20)] int numberOfNodes,
            [Values(1, 10, 100)] int sizeMBs)
        {
            for (var i = 0; i < numberOfNodes; i++) SetupCodexNode();

            PeerDownloadTestHelpers.AssertFullDownloadInterconnectivity(GetAllOnlineCodexNodes(), sizeMBs.MB());
        }
    }
}
