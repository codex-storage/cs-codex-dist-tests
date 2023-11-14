using CodexPlugin;
using NUnit.Framework;
using CodexContractsPlugin;
using Utils;

namespace CodexTests.MarketplaceTests
{
    [TestFixture]
    public class InvalidProofsTests : CodexDistTest
    {
        [Test]
        public void HostThatMissesProofsIsPaidOutLessThanHostThatDoesNotMissProofs()
        {
            var sellerInitialBalance = 234.TestTokens();
            var buyerInitialBalance = 1000.TestTokens();

            Log("deploying seller...");
            var seller = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace, "marketplace", "sales", "proving", "reservations", "node", "JSONRPC-HTTP-CLIENT", "JSONRPC-WS-CLIENT", "ethers", "clock")
                .WithStorageQuota(20.GB())
                .EnableMarketplace(sellerInitialBalance)
                .WithName("seller"));
            Log("seller deployed");
            var sellerWithFailures = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace, "marketplace", "sales", "proving", "reservations", "node", "JSONRPC-HTTP-CLIENT", "JSONRPC-WS-CLIENT", "ethers", "clock")
                .WithStorageQuota(20.GB())
                .WithBootstrapNode(seller)
                .WithSimulateProofFailures(2)
                .EnableMarketplace(sellerInitialBalance)
                .WithName("seller-with-failures"));
            Log("seller with failures deployed");

            var buyer = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace, "marketplace", "purchasing", "node", "restapi")
                .WithBootstrapNode(seller)
                .EnableMarketplace(buyerInitialBalance)
                .WithName("buyer"));
            Log("buyer deployed");

            var validator = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace, "validator")
                .WithBootstrapNode(seller)
                // .WithValidator()
                .EnableMarketplace(0.TestTokens(), 2.Eth(), true)
                .WithName("validator"));
            Log("validator deployed");

            seller.Marketplace.AssertThatBalance(Is.EqualTo(sellerInitialBalance));
            sellerWithFailures.Marketplace.AssertThatBalance(Is.EqualTo(sellerInitialBalance));
            buyer.Marketplace.AssertThatBalance(Is.EqualTo(buyerInitialBalance));

            seller.Marketplace.MakeStorageAvailable(
                size: 10.GB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            sellerWithFailures.Marketplace.MakeStorageAvailable(
                size: 10.GB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var fileSize = 10.MB();
            var testFile = GenerateTestFile(fileSize);
            var contentId = buyer.UploadFile(testFile);

            var purchaseContract = buyer.Marketplace.RequestStorage(
                contentId,
                pricePerSlotPerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 2,
                proofProbability: 2,
                duration: TimeSpan.FromMinutes(3));

            // sleep only to build up the geth logs
            Time.Sleep(TimeSpan.FromMinutes(3));
            new List<IOnlineCodexNode>(){seller, sellerWithFailures}.ForEach(node => node.DownloadGethLog());

            // seller.Marketplace.AssertThatBalance(Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            purchaseContract.WaitForStorageContractFinished(fileSize);

            // var sellerBalance = seller.Marketplace.GetBalance();
            sellerWithFailures.Marketplace.AssertThatBalance(Is.LessThan(seller.Marketplace.GetBalance()), "Seller that was slashed should have less balance than seller that was not slashed.");

            new List<IOnlineCodexNode>(){seller, sellerWithFailures, buyer, validator}.ForEach(node => node.DownloadLog());
        }
    }
}
