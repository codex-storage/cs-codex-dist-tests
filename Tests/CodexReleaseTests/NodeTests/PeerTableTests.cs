using CodexPlugin;
using CodexTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace CodexReleaseTests.NodeTests
{
    [TestFixture]
    public class PeerTableTests : AutoBootstrapDistTest
    {
        [Test]
        public void PeerTableCompleteness()
        {
            var nodes = StartCodex(10);

            var retry = new Retry(
                description: nameof(PeerTableCompleteness),
                maxTimeout: TimeSpan.FromMinutes(2),
                sleepAfterFail: TimeSpan.FromSeconds(5),
                onFail: f => { }
            );

            retry.Run(() => AssertAllNodesSeeEachOther(nodes));
        }

        private void AssertAllNodesSeeEachOther(ICodexNodeGroup nodes)
        {
            foreach (var a in nodes)
            {
                AssertHasSeenAllOtherNodes(a, nodes);
            }
        }

        private void AssertHasSeenAllOtherNodes(ICodexNode node, ICodexNodeGroup nodes)
        {
            var localNode = node.GetDebugInfo().Table.LocalNode;

            foreach (var other in nodes)
            {
                var info = other.GetDebugInfo();
                if (info.Table.LocalNode.PeerId != localNode.PeerId)
                {
                    AssertContainsPeerId(info, localNode.PeerId);
                }
            }
        }

        private void AssertContainsPeerId(DebugInfo info, string peerId)
        {
            var entry = info.Table.Nodes.SingleOrDefault(n => n.PeerId == peerId);
            if (entry == null) throw new Exception("Table entry not found.");
            if (!entry.Seen) throw new Exception("Peer not seen.");
        }
    }
}
