using DistTestCore;
using NUnit.Framework;

namespace Tests.DownloadConnectivityTests
{
    [TestFixture]
    public class FullyConnectedDownloadTests : AutoBootstrapDistTest
    {
        [TestCase(3)]
        [TestCase(10)]
        [TestCase(20)]
        public void FullyConnectedDownloadTest(int numberOfNodes)
        {
            for (var i = 0; i < numberOfNodes; i++) SetupCodexNode();

            PeerDownloadTestHelpers.AssertFullDownloadInterconnectivity(GetAllOnlineCodexNodes());
        }
    }
}
