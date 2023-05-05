using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class MarketplaceTests : DistTest
    {
        [Test]
        public void HostThatMissesProofsIsPaidOutLessThanHostThatDoesNotMissProofs()
        {
            var sellerInitialBalance = 234.TestTokens();
            var buyerInitialBalance = 1000.TestTokens();

            var sellerWithFailures = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace, new List<string>(){"market", "proving"})
                .WithStorageQuota(11.GB())
                .WithSimulateProofFailures(3)
                .EnableMarketplace(sellerInitialBalance)
                .WithName("seller with failures"));

            var seller = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace, new List<string>(){"market", "proving"})
                .WithStorageQuota(11.GB())
                .EnableMarketplace(sellerInitialBalance)
                .WithName("seller"));

            var buyer = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace)
                .WithBootstrapNode(seller)
                .EnableMarketplace(buyerInitialBalance)
                .WithName("buyer"));

            var validator = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace, new List<string>(){"validator"})
                .WithBootstrapNode(seller)
                .WithValidator()
                .WithName("validator"));

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

            var testFile = GenerateTestFile(10.MB());
            var contentId = buyer.UploadFile(testFile);

            buyer.Marketplace.RequestStorage(contentId,
                pricePerBytePerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 2,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(2));

            // Time.Sleep(TimeSpan.FromMinutes(1));

            // seller.Marketplace.AssertThatBalance(Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            Time.Sleep(TimeSpan.FromMinutes(3));

            var sellerBalance = seller.Marketplace.GetBalance();
            sellerWithFailures.Marketplace.AssertThatBalance(Is.LessThan(sellerBalance), "Seller that was slashed should have less balance than seller that was not slashed.");
        }
    }
}
