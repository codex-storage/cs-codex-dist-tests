using CodexContractsPlugin;
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
            var primary = AddCodex(s => s.WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Warn, CodexLogLevel.Warn)));

            var cid = primary.UploadFile(GenerateTestFile(5.MB()));

            var content = primary.LocalFiles();
            CollectionAssert.Contains(content.Select(c => c.Cid), cid);

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
            var contracts = Ci.StartCodexContracts(geth);

            var seller = AddCodex(s => s
                .WithStorageQuota(11.GB())
                .EnableMarketplace(geth, contracts, initialEth: 10.Eth(), initialTokens: sellerInitialBalance, isValidator: true)
                .WithSimulateProofFailures(failEveryNProofs: 3));

            AssertBalance(contracts, seller, Is.EqualTo(sellerInitialBalance));
            seller.Marketplace.MakeStorageAvailable(
                size: 10.GB(),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var testFile = GenerateTestFile(fileSize);

            var buyer = AddCodex(s => s
                            .WithBootstrapNode(seller)
                            .EnableMarketplace(geth, contracts, initialEth: 10.Eth(), initialTokens: buyerInitialBalance));

            AssertBalance(contracts, buyer, Is.EqualTo(buyerInitialBalance));

            var contentId = buyer.UploadFile(testFile);
            var purchaseContract = buyer.Marketplace.RequestStorage(contentId,
                pricePerSlotPerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 1,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(1));

            purchaseContract.WaitForStorageContractStarted(fileSize);

            AssertBalance(contracts, seller, Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            purchaseContract.WaitForStorageContractFinished();

            AssertBalance(contracts, seller, Is.GreaterThan(sellerInitialBalance), "Seller was not paid for storage.");
            AssertBalance(contracts, buyer, Is.LessThan(buyerInitialBalance), "Buyer was not charged for storage.");

            CheckLogForErrors(seller, buyer);
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
