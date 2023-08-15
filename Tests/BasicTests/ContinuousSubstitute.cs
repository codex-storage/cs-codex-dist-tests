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
                    .WithBlockTTL(TimeSpan.FromMinutes(2))
                    .WithBlockMaintenanceInterval(TimeSpan.FromMinutes(5))
                    .WithBlockMaintenanceNumber(10000)
                    .WithStorageQuota(1000.MB()));

            var nodes = group.Cast<OnlineCodexNode>().ToArray();

            var endTime = DateTime.UtcNow + TimeSpan.FromHours(1);
            while (DateTime.UtcNow < endTime)
            {
                foreach (var node in nodes)
                {
                    try
                    {
                        var file = GenerateTestFile(80.MB());
                        var cid = node.UploadFile(file);

                        var dl = node.DownloadContent(cid);
                        file.AssertIsEqual(dl);
                    }
                    catch
                    {
                        Log("Test failed. Delaying shut-down by 30 seconds to collect metrics.");
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                        throw;
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }
    }
}
