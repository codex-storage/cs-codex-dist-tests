using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class ExampleTests : DistTest
    {
        [Test]
        public void CodexLogExample()
        {
            var primary = SetupCodexNode();

            primary.UploadFile(GenerateTestFile(5.MB()));

            var log = primary.DownloadLog();

            log.AssertLogContains("Uploaded file");
        }

        [Test]
        public void TwoMetricsExample()
        {
            var group = SetupCodexNodes(2, s => s.EnableMetrics());
            var group2 = SetupCodexNodes(2, s => s.EnableMetrics());

            var primary = group[0];
            var secondary = group[1];
            var primary2 = group2[0];
            var secondary2 = group2[1];

            primary.ConnectToPeer(secondary);
            primary2.ConnectToPeer(secondary2);

            Thread.Sleep(TimeSpan.FromMinutes(2));

            primary.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
            primary2.Metrics.AssertThat("libp2p_peers", Is.EqualTo(1));
        }

        [Test]
        public void MarketplaceExample()
        {
            var sellerInitialBalance = 234.TestTokens();
            var buyerInitialBalance = 1000.TestTokens();

            var seller = SetupCodexNode(s => s
                            .WithStorageQuota(11.GB())
                            .EnableMarketplace(sellerInitialBalance));

            seller.Marketplace.AssertThatBalance(Is.EqualTo(sellerInitialBalance));
            seller.Marketplace.MakeStorageAvailable(
                size: 10.GB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var testFile = GenerateTestFile(10.MB());

            var buyer = SetupCodexNode(s => s
                .WithBootstrapNode(seller)
                .EnableMarketplace(buyerInitialBalance));
            
            var contentId = buyer.UploadFile(testFile);
            buyer.Marketplace.RequestStorage(contentId,
                pricePerBytePerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 1,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(1));

            Time.Sleep(TimeSpan.FromSeconds(10));

            seller.Marketplace.AssertThatBalance(Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            Time.Sleep(TimeSpan.FromMinutes(1));

            seller.Marketplace.AssertThatBalance(Is.GreaterThan(sellerInitialBalance), "Seller was not paid for storage.");
            buyer.Marketplace.AssertThatBalance(Is.LessThan(buyerInitialBalance), "Buyer was not charged for storage.");
        }
    }
}
