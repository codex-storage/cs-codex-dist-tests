using CodexPlugin;
using DistTestCore;
using GethPlugin;
using MetricsPlugin;
using Nethereum.JsonRpc.Client;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class ExampleTests : CodexDistTest
    {
        [Test]
        public void A()
        {
            var oneMb = GenerateTestFile(1.MB(), "oneMB");
            var fiveMb = GenerateTestFile(5.MB(), "fiveMb");
            var tenMb = GenerateTestFile(10.MB(), "tenMb");
            var hundredMb = GenerateTestFile(100.MB(), "hundredMb");
            var oneGb = GenerateTestFile(1.GB(), "oneGb");

            var a = 0;
        }

        [Test]
        public void CodexLogExample()
        {
            var primary = StartCodex(s => s.WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Warn, CodexLogLevel.Warn)));

            var cid = primary.UploadFile(GenerateTestFile(5.MB()));

            var localDatasets = primary.LocalFiles();
            CollectionAssert.Contains(localDatasets.Content.Select(c => c.Cid), cid);

            var nameMap = new Dictionary<string, string>();
            AddNameMapping(nameMap, primary);

            Get().Replacer = line =>
            {
                if (line == null) return null;
                foreach (var pair in nameMap)
                {
                    line = line.Replace(pair.Key, pair.Value);
                }
                return line;
            };


            var log = Ci.DownloadLog(primary);

            log.AssertLogContains("Uploaded file");
        }


        private void AddNameMapping(Dictionary<string, string> nameMap, ICodexNode node)
        {
            var name = node.GetName();
            var info = node.GetDebugInfo();
            var nodeId = info.Table.LocalNode.NodeId;
            var peerId = info.Table.LocalNode.PeerId;

            nameMap.Add(nodeId, name);
            nameMap.Add(peerId, name);
            nameMap.Add(CodexUtils.ToShortId(nodeId), name);
            nameMap.Add(CodexUtils.ToShortId(peerId), name);
            nameMap.Add(CodexUtils.ToNodeIdShortId(nodeId), name);
            nameMap.Add(CodexUtils.ToNodeIdShortId(peerId), name);
        }

        [Test]
        public void TwoMetricsExample()
        {
            var group = StartCodex(2, s => s.EnableMetrics());
            var group2 = StartCodex(2, s => s.EnableMetrics());

            var primary = group[0];
            var secondary = group[1];
            var primary2 = group2[0];
            var secondary2 = group2[1];

            var metrics = Ci.GetMetricsFor(primary, primary2);

            primary.ConnectToPeer(secondary);
            primary2.ConnectToPeer(secondary2);

            Thread.Sleep(TimeSpan.FromMinutes(2));

            metrics[0].AssertThat("libp2p_peers", Is.EqualTo(1));
            metrics[1].AssertThat("libp2p_peers", Is.EqualTo(1));

            LogNodeStatus(primary, metrics[0]);
            LogNodeStatus(primary2, metrics[1]);
        }

        [Test]
        public void GethBootstrapTest()
        {
            var boot = Ci.StartGethNode(s => s.WithName("boot").IsMiner());
            var disconnected = Ci.StartGethNode(s => s.WithName("disconnected"));
            var follow = Ci.StartGethNode(s => s.WithBootstrapNode(boot).WithName("follow"));

            Thread.Sleep(12000);

            var bootN = boot.GetSyncedBlockNumber();
            var discN = disconnected.GetSyncedBlockNumber();
            var followN = follow.GetSyncedBlockNumber();

            Assert.That(bootN, Is.EqualTo(followN));
            Assert.That(discN, Is.LessThan(bootN));
        }
    }
}
