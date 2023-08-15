using DistTestCore;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class ContinuousSubstitute : AutoBootstrapDistTest
    {
        [Test]
        [UseLongTimeouts]
        public void ContinuousTestSubstitute()
        {
            var group = SetupCodexNodes(5, o => o
                    .EnableMetrics()
                    .EnableMarketplace(100000.TestTokens(), 0.Eth(), isValidator: true)
                    .WithBlockTTL(TimeSpan.FromMinutes(2))
                    .WithStorageQuota(3.GB()));

            var nodes = group.Cast<OnlineCodexNode>().ToArray();

            foreach (var node in nodes)
            {
                node.Marketplace.MakeStorageAvailable(
                size: 1.GB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 1024.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(5));
            }

            var endTime = DateTime.UtcNow + TimeSpan.FromHours(10);
            while (DateTime.UtcNow < endTime)
            {
                var allNodes = nodes.ToList();
                var primary = allNodes.PickOneRandom();
                var secondary = allNodes.PickOneRandom();

                Log("Run Test");
                PerformTest(primary, secondary);

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private ByteSize fileSize = 80.MB();

        private void PerformTest(IOnlineCodexNode primary, IOnlineCodexNode secondary)
        {
            ScopedTestFiles(() =>
            {
                var testFile = GenerateTestFile(fileSize);

                var contentId = primary.UploadFile(testFile);

                var downloadedFile = secondary.DownloadContent(contentId);

                testFile.AssertIsEqual(downloadedFile);
            });
        }
        
        [Test]
        [UseLongTimeouts]
        public void HoldMyBeerTest()
        {
            var group = SetupCodexNodes(5, o => o
                    .EnableMetrics()
                    //.EnableMarketplace(100000.TestTokens(), 0.Eth(), isValidator: true)
                    .WithBlockTTL(TimeSpan.FromMinutes(2))
                    .WithBlockMaintenanceInterval(TimeSpan.FromMinutes(5))
                    .WithBlockMaintenanceNumber(10000)
                    .WithStorageQuota(500.MB()));

            var nodes = group.Cast<OnlineCodexNode>().ToArray();

            //foreach (var node in nodes)
            //{
            //    node.Marketplace.MakeStorageAvailable(
            //    size: 1.GB(),
            //    minPricePerBytePerSecond: 1.TestTokens(),
            //    maxCollateral: 1024.TestTokens(),
            //    maxDuration: TimeSpan.FromMinutes(5));
            //}

            //Thread.Sleep(2000);

            //Log("calling crash...");
            //var http = new Http(Get().Log, Get().TimeSet, nodes.First().CodexAccess.Address, baseUrl: "/api/codex/v1", nodes.First().CodexAccess.Container.Name);
            //var str = http.HttpGetString("debug/crash");

            //Log("crash called.");

            //Thread.Sleep(TimeSpan.FromSeconds(60));

            //Log("test done.");

            var endTime = DateTime.UtcNow + TimeSpan.FromHours(2);
            while (DateTime.UtcNow < endTime)
            {
                foreach (var node in nodes)
                {
                    var file = GenerateTestFile(80.MB());
                    var cid = node.UploadFile(file);

                    var dl = node.DownloadContent(cid);
                    file.AssertIsEqual(dl);
                }

                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
        }
    }
}
