using DistTestCore;
using NUnit.Framework;

namespace Tests.PeerDiscoveryTests
{
    [TestFixture]
    public class PeerDiscoveryTests : AutoBootstrapDistTest
    {
        [Test]
        public void TwoNodes()
        {
            SetupCodexNode();

            AssertAllNodesConnected();
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
        public void VariableNodesInPods(int number)
        {
            for (var i = 0; i < number; i++)
            {
                SetupCodexNode();
            }

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            PeerTestHelpers.AssertFullyConnected(GetAllOnlineCodexNodes(), GetTestLog());
        }
    }
}
