using DistTestCore.Codex;
using DistTestCore.Helpers;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class PeersTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => -1;
        public override TimeSpan RunTestEvery => TimeSpan.FromSeconds(30);
        public override TestFailMode TestFailMode => TestFailMode.AlwaysRunAllMoments;

        [TestMoment(t: 0)]
        public void CheckConnectivity()
        {
            var checker = new PeerConnectionTestHelpers(Log);
            checker.AssertFullyConnected(Nodes);
        }

        [TestMoment(t: 10)]
        public void CheckRoutingTables()
        {
            var allInfos = Nodes.Select(n =>
            {
                var info = n.GetDebugInfo();
                Log.Log($"{n.GetName()} = {info.table.localNode.nodeId}");
                Log.AddStringReplace(info.table.localNode.nodeId, n.GetName());
                return info;
            }).ToArray();

            var allIds = allInfos.Select(i => i.table.localNode.nodeId).ToArray();
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
                var nl = Environment.NewLine;
                return $"{nl}At node '{info.table.localNode.nodeId}'{nl}" +
                    $"Not all of{nl}'{string.Join(",", expected)}'{nl}" +
                    $"were present in routing table:{nl}'{string.Join(",", known)}'";
            }

            return string.Empty;
        }
    }
}
