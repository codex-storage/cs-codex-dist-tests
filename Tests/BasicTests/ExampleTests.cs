using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace Tests.BasicTests
{
    [TestFixture]
    public class ExampleTests : DistTest
    {
        [Test]
        public void CodexLogExample()
        {
            var primary = SetupCodexNodes(1)
                            .WithLogLevel(CodexLogLevel.Trace)
                            .BringOnline()[0];

            primary.UploadFile(GenerateTestFile(5.MB()));

            var log = primary.DownloadLog();

            log.AssertLogContains("Uploaded file");
        }

        [Test]
        public void TwoMetricsExample()
        {
            var group = SetupCodexNodes(2)
                        .EnableMetrics()
                        .BringOnline();

            var group2 = SetupCodexNodes(2)
                        .EnableMetrics()
                        .BringOnline();

            var primary = group[0];
            var secondary = group[1];
            var primary2 = group2[0];
            var secondary2 = group2[1];

            primary.ConnectToPeer(secondary);
            primary2.ConnectToPeer(secondary2);

            Thread.Sleep(TimeSpan.FromMinutes(5));

            primary.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
            primary2.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
        }

        [Test]
        public void MarketplaceExample()
        {
            var group = SetupCodexNodes(2)
                            .WithStorageQuota(10.GB())
                            .EnableMarketplace(20.TestTokens())
                            .BringOnline();

            foreach (var node in group)
            {
                Assert.That(node.Marketplace.GetBalance(), Is.EqualTo(20));
            }

            // WIP: Balance is now only ETH.
            // todo: All nodes should have plenty of ETH to pay for transactions.
            // todo: Upload our own token, use this exclusively. ETH should be invisibile to the tests.


            //var secondary = SetupCodexNodes(1)
            //                .EnableMarketplace(initialBalance: 1000)
            //                .BringOnline()[0];

            //primary.ConnectToPeer(secondary);
            //primary.Marketplace.MakeStorageAvailable(10.GB(), minPricePerBytePerSecond: 1, maxCollateral: 20);

            //var testFile = GenerateTestFile(10.MB());
            //var contentId = secondary.UploadFile(testFile);
            //secondary.Marketplace.RequestStorage(contentId, pricePerBytePerSecond: 2,
            //    requiredCollateral: 10, minRequiredNumberOfNodes: 1);

            //primary.Marketplace.AssertThatBalance(Is.LessThan(20), "Collateral was not placed.");
            //var primaryBalance = primary.Marketplace.GetBalance();

            //secondary.Marketplace.AssertThatBalance(Is.LessThan(1000), "Contractor was not charged for storage.");
            //primary.Marketplace.AssertThatBalance(Is.GreaterThan(primaryBalance), "Storer was not paid for storage.");
        }
    }
}
