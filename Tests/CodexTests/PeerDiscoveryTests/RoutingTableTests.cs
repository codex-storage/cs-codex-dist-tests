using CodexPlugin;
using NUnit.Framework;
using Utils;

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
            Time.Retry(CheckRoutingTable, 3, nameof(CheckRoutingTable));
        }

        private void CheckRoutingTable()
        {
            var all = GetAllOnlineCodexNodes();
            var allResponses = all.Select(n => n.GetDebugInfo()).ToArray();

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

        private string AreAllPresent(CodexDebugResponse info, CodexDebugResponse[] allResponses)
        {
            var knownIds = info.table.nodes.Select(n => n.nodeId).ToArray();
            var allOthers = GetAllOtherResponses(info, allResponses);
            var expectedIds = allOthers.Select(i => i.table.localNode.nodeId).ToArray();

            if (!expectedIds.All(ex => knownIds.Contains(ex)))
            {
                return $"Not all of '{string.Join(",", expectedIds)}' were present in routing table: '{string.Join(",", knownIds)}'";
            }

            return string.Empty;
        }

        private CodexDebugResponse[] GetAllOtherResponses(CodexDebugResponse exclude, CodexDebugResponse[] allResponses)
        {
            return allResponses.Where(r => r.id != exclude.id).ToArray();
        }
    }
}
