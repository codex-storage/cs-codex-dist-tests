using CodexClient;
using CodexTests;
using CodexTests.Helpers;
using NUnit.Framework;

namespace CodexReleaseTests.NodeTests
{
    [TestFixture]
    public class PeerTableTests : AutoBootstrapDistTest
    {
        [Test]
        public void PeerTableCompleteness()
        {
            var nodes = StartCodex(10);

            AssertAllNodesSeeEachOther(nodes.Concat([BootstrapNode!]));
        }

        private void AssertAllNodesSeeEachOther(IEnumerable<ICodexNode> nodes)
        {
            var helper = new PeerConnectionTestHelpers(GetTestLog());
            helper.AssertFullyConnected(nodes);
        }
    }
}
