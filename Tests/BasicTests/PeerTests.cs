using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace Tests.BasicTests
{
    [TestFixture]
    public class PeerTests : DistTest
    {
        [Test]
        public void TwoNodes()
        {
            var primary = SetupCodexBootstrapNode();
            var secondary = SetupCodexNode(s => s.WithBootstrapNode(primary));

            primary.ConnectToPeer(secondary); // TODO REMOVE THIS: This is required for the switchPeers to show up.

            // This is required for the enginePeers to show up.
            //var file = GenerateTestFile(10.MB());
            //var contentId = primary.UploadFile(file);
            //var file2 = secondary.DownloadContent(contentId);
            //file.AssertIsEqual(file2);

            AssertKnowEachother(primary, secondary);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void VariableNodes(int number)
        {
            var bootstrap = SetupCodexBootstrapNode();
            var nodes = SetupCodexNodes(number, s => s.WithBootstrapNode(bootstrap));

            var file = GenerateTestFile(10.MB());
            var contentId = nodes.First().UploadFile(file);
            var file2 = nodes.Last().DownloadContent(contentId);
            file.AssertIsEqual(file2);

            // <TODO REMOVE THIS>
            foreach (var node in nodes) bootstrap.ConnectToPeer(node);
            for (var x = 0; x < number; x++)
            {
                for (var y = x + 1; y < number; y++)
                {
                    nodes[x].ConnectToPeer(nodes[y]);
                }
            }
            // </TODO REMOVE THIS>

            foreach (var node in nodes) AssertKnowEachother(node, bootstrap);

            for (var x = 0; x < number; x++)
            {
                for (var y = x + 1; y < number; y++)
                {
                    AssertKnowEachother(nodes[x], nodes[y]);
                }
            }
        }

        private void AssertKnowEachother(IOnlineCodexNode a, IOnlineCodexNode b)
        {
            AssertKnowEachother(a.GetDebugInfo(), b.GetDebugInfo());
        }

        private void AssertKnowEachother(CodexDebugResponse a, CodexDebugResponse b)
        {
            AssertKnows(a, b);
            AssertKnows(b, a);
        }

        private void AssertKnows(CodexDebugResponse a, CodexDebugResponse b)
        {
            //var enginePeers = string.Join(",", a.enginePeers.Select(p => p.peerId));
            var switchPeers = string.Join(",", a.switchPeers.Select(p => p.peerId));

            //Debug($"Looking for {b.id} in engine-peers [{enginePeers}]");
            Debug($"{a.id} is looking for {b.id} in switch-peers [{switchPeers}]");

            //Assert.That(a.enginePeers.Any(p => p.peerId == b.id), $"{a.id} was looking for '{b.id}' in engine-peers [{enginePeers}] but it was not found.");
            Assert.That(a.switchPeers.Any(p => p.peerId == b.id), $"{a.id} was looking for '{b.id}' in switch-peers [{switchPeers}] but it was not found.");
        }
    }
}
