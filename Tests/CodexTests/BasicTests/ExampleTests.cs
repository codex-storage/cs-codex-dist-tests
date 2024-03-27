using CodexContractsPlugin;
using CodexPlugin;
using DistTestCore;
using GethPlugin;
using MetricsPlugin;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;
using Request = CodexContractsPlugin.Marketplace.Request;

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
            var buyerInitialBalance = 100000.TestTokens();
            var fileSize = 10.MB();

            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);

            var seller = AddCodex(s => s
                .WithName("Seller")
                .WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Error, CodexLogLevel.Error, CodexLogLevel.Warn)
                {
                    ContractClock = CodexLogLevel.Trace,
                })
                .WithStorageQuota(11.GB())
                .EnableMarketplace(geth, contracts, m => m
                    .WithInitial(10.Eth(), sellerInitialBalance)
                    .AsStorageNode()
                    .AsValidator()));

            AssertBalance(contracts, seller, Is.EqualTo(sellerInitialBalance));

            var availability = new StorageAvailability(
                totalSpace: 10.GB(),
                maxDuration: TimeSpan.FromMinutes(30),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens()
            );
            seller.Marketplace.MakeStorageAvailable(availability);

            var testFile = GenerateTestFile(fileSize);

            var buyer = AddCodex(s => s
                .WithName("Buyer")
                .WithBootstrapNode(seller)
                .EnableMarketplace(geth, contracts, m => m
                    .WithInitial(10.Eth(), buyerInitialBalance)));

            AssertBalance(contracts, buyer, Is.EqualTo(buyerInitialBalance));

            var contentId = buyer.UploadFile(testFile);

            var purchase = new StoragePurchase(contentId)
            {
                PricePerSlotPerSecond = 2.TestTokens(),
                RequiredCollateral = 10.TestTokens(),
                MinRequiredNumberOfNodes = 5,
                NodeFailureTolerance = 2,
                ProofProbability = 5,
                Duration = TimeSpan.FromMinutes(5),
                Expiry = TimeSpan.FromMinutes(4)
            };

            var purchaseContract = buyer.Marketplace.RequestStorage(purchase);

            purchaseContract.WaitForStorageContractStarted();

            AssertBalance(contracts, seller, Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            var request = GetOnChainStorageRequest(contracts);
            AssertStorageRequest(request, purchase, contracts, buyer);
            AssertSlotFilledEvents(contracts, purchase, request, seller);
            AssertContractSlot(contracts, request, 0, seller);

            purchaseContract.WaitForStorageContractFinished();

            AssertBalance(contracts, seller, Is.GreaterThan(sellerInitialBalance), "Seller was not paid for storage.");
            AssertBalance(contracts, buyer, Is.LessThan(buyerInitialBalance), "Buyer was not charged for storage.");
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Finished));
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

        private void AssertSlotFilledEvents(ICodexContracts contracts, StoragePurchase purchase, Request request, ICodexNode seller)
        {
            // Expect 1 fulfilled event for the purchase.
            var requestFulfilledEvents = contracts.GetRequestFulfilledEvents(GetTestRunTimeRange());
            Assert.That(requestFulfilledEvents.Length, Is.EqualTo(1));
            CollectionAssert.AreEqual(request.RequestId, requestFulfilledEvents[0].RequestId);

            // Expect 1 filled-slot event for each slot in the purchase.
            var filledSlotEvents = contracts.GetSlotFilledEvents(GetTestRunTimeRange());
            Assert.That(filledSlotEvents.Length, Is.EqualTo(purchase.MinRequiredNumberOfNodes));
            for (var i = 0; i < purchase.MinRequiredNumberOfNodes; i++)
            {
                var filledSlotEvent = filledSlotEvents.Single(e => e.SlotIndex == i);
                Assert.That(filledSlotEvent.RequestId.ToHex(), Is.EqualTo(request.RequestId.ToHex()));
                Assert.That(filledSlotEvent.Host, Is.EqualTo(seller.EthAddress));
            }
        }

        private void AssertStorageRequest(Request request, StoragePurchase purchase, ICodexContracts contracts, ICodexNode buyer)
        {
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Started));
            Assert.That(request.ClientAddress, Is.EqualTo(buyer.EthAddress));
            Assert.That(request.Ask.Slots, Is.EqualTo(purchase.MinRequiredNumberOfNodes));
        }

        private Request GetOnChainStorageRequest(ICodexContracts contracts)
        {
            var requests = contracts.GetStorageRequests(GetTestRunTimeRange());
            Assert.That(requests.Length, Is.EqualTo(1));
            return requests.Single();
        }

        private void AssertContractSlot(ICodexContracts contracts, Request request, int contractSlotIndex, ICodexNode expectedSeller)
        {
            var slotHost = contracts.GetSlotHost(request, contractSlotIndex);
            Assert.That(slotHost, Is.EqualTo(expectedSeller.EthAddress));
        }
    }
}
