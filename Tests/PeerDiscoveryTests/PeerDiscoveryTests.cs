using DistTestCore;
using NUnit.Framework;

namespace Tests.PeerDiscoveryTests
{
    [TestFixture]
    public class PeerDiscoveryTests : AutoBootstrapDistTest
    {
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
        public void StagedVariableNodes(int numberOfNodes, int numberOfStages)
        {
            for (var i = 0; i < numberOfStages; i++)
            {
                SetupCodexNodes(numberOfNodes);

                AssertAllNodesConnected();
            }
        }

        private void AssertAllNodesConnected()
        {
            PeerTestHelpers.AssertFullyConnected(GetAllOnlineCodexNodes(), GetTestLog());
        }
    }
}
