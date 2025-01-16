using CodexPlugin;
using DistTestCore;
using GethPlugin;
using MetricsPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class ExampleTests : CodexDistTest
    {
        [Test]
        public void CodexLogExample()
        {
            var primary = StartCodex(s => s.WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Warn, CodexLogLevel.Warn)));

            var cid = primary.UploadFile(GenerateTestFile(5.MB()));

            var localDatasets = primary.LocalFiles();
            CollectionAssert.Contains(localDatasets.Content.Select(c => c.Cid), cid);

            var log = primary.DownloadLog();

            log.AssertLogContains("Uploaded file");
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

            var metrics = Ci.GetMetricsFor(scrapeInterval: TimeSpan.FromSeconds(10), primary, primary2);

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
            var boot = StartGethNode(s => s.WithName("boot").IsMiner());
            var disconnected = StartGethNode(s => s.WithName("disconnected"));
            var follow = StartGethNode(s => s.WithBootstrapNode(boot).WithName("follow"));

            Thread.Sleep(12000);

            var bootN = boot.GetSyncedBlockNumber();
            var discN = disconnected.GetSyncedBlockNumber();
            var followN = follow.GetSyncedBlockNumber();

            Assert.That(bootN, Is.EqualTo(followN));
            Assert.That(discN, Is.LessThan(bootN));
        }
    }
}
