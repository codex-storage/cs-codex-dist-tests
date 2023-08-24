using DistTestCore;
using NUnit.Framework;

namespace Tests.PeerDiscoveryTests
{
    [TestFixture]
    public class PeerDiscoveryTests : AutoBootstrapDistTest
    {
        [Test]
        public void CanReportUnknownPeerId()
        {
            var unknownId = "16Uiu2HAkv2CHWpff3dj5iuVNERAp8AGKGNgpGjPexJZHSqUstfsK";
            var node = SetupCodexNode();

            var result = node.GetDebugPeer(unknownId);
            Assert.That(result.IsPeerFound, Is.False);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        [TestCase(20)]
        public void VariableNodes(int number)
        {
            SetupCodexNodes(number);

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            CreatePeerConnectionTestHelpers().AssertFullyConnected(GetAllOnlineCodexNodes());
        }
    }
}
