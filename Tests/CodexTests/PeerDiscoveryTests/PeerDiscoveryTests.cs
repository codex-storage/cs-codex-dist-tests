using CodexContractsPlugin;
using CodexPlugin;
using GethPlugin;
using NUnit.Framework;

namespace CodexTests.PeerDiscoveryTests
{
    [TestFixture]
    public class PeerDiscoveryTests : AutoBootstrapDistTest
    {
        [Test]
        public void CanReportUnknownPeerId()
        {
            var unknownId = "16Uiu2HAkv2CHWpff3dj5iuVNERAp8AGKGNgpGjPexJZHSqUstfsK";
            var node = AddCodex();

            var result = node.GetDebugPeer(unknownId);
            Assert.That(result.IsPeerFound, Is.False);
        }

        [Test]
        public void MetricsDoesNotInterfereWithPeerDiscovery()
        {
            AddCodex(2, s => s.EnableMetrics());

            AssertAllNodesConnected();
        }

        [Test]
        public void MarketplaceDoesNotInterfereWithPeerDiscovery()
        {
            var geth = Ci.StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth);
            AddCodex(2, s => s.EnableMarketplace(geth, contracts, 10.Eth(), 1000.TestTokens()));

            AssertAllNodesConnected();
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void VariableNodes(int number)
        {
            AddCodex(number);

            AssertAllNodesConnected();
        }

        private void AssertAllNodesConnected()
        {
            var allNodes = GetAllOnlineCodexNodes();
            CreatePeerConnectionTestHelpers().AssertFullyConnected(allNodes);
            CheckRoutingTable(allNodes);
        }

        private void CheckRoutingTable(IEnumerable<ICodexNode> allNodes)
        {
            var allResponses = allNodes.Select(n => n.GetDebugInfo()).ToArray();

            var errors = new List<string>();
            foreach (var response in allResponses)
            {
                var error = AreAllPresent(response, allResponses);
                if (!string.IsNullOrEmpty(error)) errors.Add(error);
            }

            if (errors.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, errors));
            }
        }

        private string AreAllPresent(DebugInfo info, DebugInfo[] allResponses)
        {
            var knownIds = info.table.nodes.Select(n => n.nodeId).ToArray();
            var allOthers = GetAllOtherResponses(info, allResponses);
            var expectedIds = allOthers.Select(i => i.table.localNode.nodeId).ToArray();

            if (!expectedIds.All(ex => knownIds.Contains(ex)))
            {
                return $"Node {info.id}: Not all of '{string.Join(",", expectedIds)}' were present in routing table: '{string.Join(",", knownIds)}'";
            }

            return string.Empty;
        }

        private DebugInfo[] GetAllOtherResponses(DebugInfo exclude, DebugInfo[] allResponses)
        {
            return allResponses.Where(r => r.Id != exclude.Id).ToArray();
        }
    }
}
