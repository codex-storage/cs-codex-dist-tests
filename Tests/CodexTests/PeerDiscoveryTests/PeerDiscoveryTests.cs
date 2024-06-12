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
            var node = StartCodex();

            var result = node.GetDebugPeer(unknownId);
            Assert.That(result.IsPeerFound, Is.False);
        }

        [Test]
        public void MetricsDoesNotInterfereWithPeerDiscovery()
        {
            var nodes = StartCodex(2, s => s.EnableMetrics());

            AssertAllNodesConnected(nodes);
        }

        [Test]
        public void MarketplaceDoesNotInterfereWithPeerDiscovery()
        {
            var geth = Ci.StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth);
            var nodes = StartCodex(2, s => s.EnableMarketplace(geth, contracts, m => m
                .WithInitial(10.Eth(), 1000.TstWei())));

            AssertAllNodesConnected(nodes);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void VariableNodes(int number)
        {
            var nodes = StartCodex(number);

            AssertAllNodesConnected(nodes);
        }

        private void AssertAllNodesConnected(IEnumerable<ICodexNode> nodes)
        {
            CreatePeerConnectionTestHelpers().AssertFullyConnected(nodes);
            CheckRoutingTable(nodes);
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
            var knownIds = info.Table.Nodes.Select(n => n.NodeId).ToArray();
            var allOthers = GetAllOtherResponses(info, allResponses);
            var expectedIds = allOthers.Select(i => i.Table.LocalNode.NodeId).ToArray();

            if (!expectedIds.All(ex => knownIds.Contains(ex)))
            {
                return $"Node {info.Id}: Not all of '{string.Join(",", expectedIds)}' were present in routing table: '{string.Join(",", knownIds)}'";
            }

            return string.Empty;
        }

        private DebugInfo[] GetAllOtherResponses(DebugInfo exclude, DebugInfo[] allResponses)
        {
            return allResponses.Where(r => r.Id != exclude.Id).ToArray();
        }
    }
}
