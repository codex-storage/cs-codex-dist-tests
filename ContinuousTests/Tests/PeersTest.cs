using DistTestCore.Codex;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class PeersTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => -1;
        public override TimeSpan RunTestEvery => TimeSpan.FromSeconds(30);
        public override TestFailMode TestFailMode => TestFailMode.AlwaysRunAllMoments;

        [TestMoment(t: 0)]
        public void CheckRoutingTables()
        {
            var allIds = Nodes.Select(n => n.GetDebugInfo().table.localNode.nodeId).ToArray();

            var errors = Nodes.Select(n => AreAllPresent(n, allIds)).Where(s => !string.IsNullOrEmpty(s)).ToArray();

            if (errors.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, errors));
            }
        }

        private string AreAllPresent(CodexAccess n, string[] allIds)
        {
            var info = n.GetDebugInfo();
            var known = info.table.nodes.Select(n => n.nodeId).ToArray();
            var expected = allIds.Where(i => i != info.table.localNode.nodeId).ToArray();

            if (!expected.All(ex => known.Contains(ex)))
            {
                return $"Not all of '{string.Join(",", expected)}' were present in routing table: '{string.Join(",", known)}'";
            }

            return string.Empty;
        }
    }
}
