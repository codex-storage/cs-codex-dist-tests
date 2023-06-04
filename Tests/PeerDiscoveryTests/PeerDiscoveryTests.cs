using DistTestCore;
using DistTestCore.Helpers;
using NUnit.Framework;
using Utils;

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
        public void VariableNodes(int number)
        {
            SetupCodexNodes(number);

            AssertAllNodesConnected();
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        [TestCase(20)]
        public void VariableNodesInPods(int number)
        {
            for (var i = 0; i < number; i++)
            {
                SetupCodexNode();
            }

            AssertAllNodesConnected();
        }

        [TestCase(3, 3)]
        [TestCase(3, 5)]
        [TestCase(3, 10)]
        [TestCase(5, 10)]
        [TestCase(3, 20)]
        [TestCase(5, 20)]
        public void StagedVariableNodes(int numberOfNodes, int numberOfStages)
        {
            for (var i = 0; i < numberOfStages; i++)
            {
                SetupCodexNodes(numberOfNodes);

                AssertAllNodesConnected();
            }

            for (int i = 0; i < 5; i++)
            {
                Time.Sleep(TimeSpan.FromSeconds(30));
                AssertAllNodesConnected();
            }
        }

        private void AssertAllNodesConnected()
        {
            PeerConnectionTestHelpers.AssertFullyConnected(GetAllOnlineCodexNodes());
        }
    }
}
