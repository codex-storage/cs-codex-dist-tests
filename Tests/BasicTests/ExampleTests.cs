using CodexContractsPlugin;
using DistTestCore;
using GethPlugin;
using MetricsPlugin;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class ExampleTests : CodexDistTest
    {
        [Test]
        public void CodexLogExample()
        {
            var primary = AddCodex();

            primary.UploadFile(GenerateTestFile(5.MB()));

            var log = Ci.DownloadLog(primary);

            log.AssertLogContains("Uploaded file");
        }

        [Test]
        public void TwoMetricsExample()
        {
            var group = AddCodex(2, s => s.EnableMetrics());
            var group2 = AddCodex(2, s => s.EnableMetrics());

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
        }

        [Test]
        public void MarketplaceExample()
        {
            var sellerInitialBalance = 234.TestTokens();
            var buyerInitialBalance = 1000.TestTokens();
            var fileSize = 10.MB();

            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.DeployCodexContracts(geth);

            var seller = AddCodex(s => s
                            .WithStorageQuota(11.GB())
                            .EnableMarketplace(geth, contracts, initialEth: 10.Eth(), initialTokens: sellerInitialBalance));
            
            AssertBalance(geth, contracts, seller, Is.EqualTo(sellerInitialBalance));
            seller.Marketplace.MakeStorageAvailable(
                size: 10.GB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var testFile = GenerateTestFile(fileSize);

            var buyer = AddCodex(s => s
                            .WithBootstrapNode(seller)
                            .EnableMarketplace(geth, contracts, initialEth: 10.Eth(), initialTokens: buyerInitialBalance));
            
            AssertBalance(geth, contracts, buyer, Is.EqualTo(buyerInitialBalance));

            var contentId = buyer.UploadFile(testFile);
            var purchaseContract = buyer.Marketplace.RequestStorage(contentId,
                pricePerSlotPerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 1,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(1));

            purchaseContract.WaitForStorageContractStarted(fileSize);

            AssertBalance(geth, contracts, seller, Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            purchaseContract.WaitForStorageContractFinished();

            AssertBalance(geth, contracts, seller, Is.GreaterThan(sellerInitialBalance), "Seller was not paid for storage.");
            AssertBalance(geth, contracts, buyer, Is.LessThan(buyerInitialBalance), "Buyer was not charged for storage.");
        }
    }
}
