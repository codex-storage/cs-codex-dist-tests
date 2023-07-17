using DistTestCore.Helpers;
using DistTestCore;
using NUnit.Framework;

namespace TestsLong.DownloadConnectivityTests
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
            for (var i = 0; i < numberOfNodes; i++) SetupCodexNode();

            PeerDownloadTestHelpers.AssertFullDownloadInterconnectivity(GetAllOnlineCodexNodes(), sizeMBs.MB());
        }
    }
}
