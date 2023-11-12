using CodexPlugin;
using NUnit.Framework;

namespace CodexTests.PeerDiscoveryTests
{
    [TestFixture]
    public class RoutingTableTests : AutoBootstrapDistTest
    {
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        [TestCase(20)]
        public void VariableNodes(int number)
        {
            AddCodex(number);

            AssertRoutingTable();
        }

        private void AssertRoutingTable()
        {
            var all = GetAllOnlineCodexNodes();
            var allNodeIds = all.Select(n => n.GetDebugInfo().table.localNode.nodeId).ToArray();

            var errors = all.Select(n => AreAllPresent(n, allNodeIds)).Where(s => !string.IsNullOrEmpty(s)).ToArray();

            if (errors.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, errors));
            }
        }

        private string AreAllPresent(ICodexNode n, string[] allNodesIds)
        {
            var info = n.GetDebugInfo();
            var knownIds = info.table.nodes.Select(n => n.nodeId).ToArray();
            var expectedIds = allNodesIds.Where(id => id != info.table.localNode.nodeId).ToArray();

            if (!expectedIds.All(ex => knownIds.Contains(ex)))
            {
                return $"Not all of '{string.Join(",", expectedIds)}' were present in routing table: '{string.Join(",", knownIds)}'";
            }

            return string.Empty;
        }
    }
}
