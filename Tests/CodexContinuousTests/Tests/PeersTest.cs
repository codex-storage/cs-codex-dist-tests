using CodexClient;
using CodexTests.Helpers;
using ContinuousTests;
using NUnit.Framework;

namespace CodexContinuousTests.Tests
{
    public class PeersTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => -1;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(2);
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
                Log.Log($"{n.GetName()} = {info.Table.LocalNode.NodeId}");
                Log.AddStringReplace(info.Table.LocalNode.NodeId, n.GetName());
                return info;
            }).ToArray();

            var allIds = allInfos.Select(i => i.Table.LocalNode.NodeId).ToArray();
            var errors = Nodes.Select(n => AreAllPresent(n, allIds)).Where(s => !string.IsNullOrEmpty(s)).ToArray();

            if (errors.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, errors));
            }
        }

        private string AreAllPresent(ICodexNode n, string[] allIds)
        {
            var info = n.GetDebugInfo();
            var known = info.Table.Nodes.Select(n => n.NodeId).ToArray();
            var expected = allIds.Where(i => i != info.Table.LocalNode.NodeId).ToArray();

            if (!expected.All(ex => known.Contains(ex)))
            {
                var nl = Environment.NewLine;
                return $"{nl}At node '{info.Table.LocalNode.NodeId}'{nl}" +
                    $"Not all of{nl}'{string.Join(",", expected)}'{nl}" +
                    $"were present in routing table:{nl}'{string.Join(",", known)}'";
            }

            return string.Empty;
        }
    }
}
