using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class SlotSelection : AutoBootstrapDistTest
    {

        [Test]
        public void RequestExpiresIfNotFilledAndMoneyAreReturned()
        {
            var sellerInitialBalance = 234.TestTokens();
            var buyerInitialBalance = 1000.TestTokens();
            var fileSize = 10.MB();

            var seller1 = SetupCodexNode(s => s
                            .WithLogLevel(CodexLogLevel.Trace, "marketplace", "sales", "proving", "reservations")
                            .WithStorageQuota(50.MB())
                            .EnableMarketplace(sellerInitialBalance)
                            .WithName("seller1"));

            seller1.Marketplace.AssertThatBalance(Is.EqualTo(sellerInitialBalance));

            // The two availabilities are needed until https://github.com/codex-storage/nim-codex/pull/535 is merged
            seller1.Marketplace.MakeStorageAvailable(
                size: 11.MB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));
            seller1.Marketplace.MakeStorageAvailable(
                size: 11.MB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var seller2 = SetupCodexNode(s => s
                            .WithLogLevel(CodexLogLevel.Trace, "marketplace", "sales", "proving", "reservations")
                            .WithStorageQuota(50.MB())
                            .EnableMarketplace(sellerInitialBalance)
                            .WithName("seller2"));

            seller2.Marketplace.AssertThatBalance(Is.EqualTo(sellerInitialBalance));
            seller2.Marketplace.MakeStorageAvailable(
                size: 11.MB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));
            seller2.Marketplace.MakeStorageAvailable(
                size: 11.MB(),
                minPricePerBytePerSecond: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var testFile = GenerateTestFile(fileSize);
            var buyer = SetupCodexNode(s => s
                .WithLogLevel(CodexLogLevel.Trace, "marketplace", "purchasing", "node", "restapi")
                .EnableMarketplace(buyerInitialBalance)
                .WithName("buyer"));

            buyer.Marketplace.AssertThatBalance(Is.EqualTo(buyerInitialBalance));

            var contentId = buyer.UploadFile(testFile);
            var purchaseContract = buyer.Marketplace.RequestStorage(contentId,
                pricePerSlotPerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 3,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(1),
                expiry: DateTime.Now.AddMinutes(2));

            Time.Sleep(TimeSpan.FromSeconds(100));

            seller1.Marketplace.AssertThatBalance(Is.LessThan(sellerInitialBalance), "Collateral was not placed.");
            seller2.Marketplace.AssertThatBalance(Is.LessThan(sellerInitialBalance), "Collateral was not placed.");
            buyer.Marketplace.AssertThatBalance(Is.LessThan(buyerInitialBalance), "Buyer was not charged for storage.");

            purchaseContract.WaitForStorageContractFailed(TimeSpan.FromSeconds(120));

            seller1.Marketplace.AssertThatBalance(Is.EqualTo(sellerInitialBalance), "Seller was not returned collateral.");
            seller2.Marketplace.AssertThatBalance(Is.EqualTo(sellerInitialBalance), "Seller was not returned collateral.");
            buyer.Marketplace.AssertThatBalance(Is.EqualTo(buyerInitialBalance), "Buyer was not returned money for the request.");
        }
    }
}
