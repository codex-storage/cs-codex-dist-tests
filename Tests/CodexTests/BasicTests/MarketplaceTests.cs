using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using GethPlugin;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class MarketplaceTests : AutoBootstrapDistTest
    {
        [Test]
        public void MarketplaceExample()
        {
            var hostInitialBalance = 234.TestTokens();
            var clientInitialBalance = 100000.TestTokens();
            var fileSize = 10.MB();

            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);

            var numberOfHosts = 3;
            for (var i = 0; i < numberOfHosts; i++)
            {
                var host = AddCodex(s => s
                    .WithName("Host")
                    .WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Error, CodexLogLevel.Error, CodexLogLevel.Warn)
                    {
                        ContractClock = CodexLogLevel.Trace,
                    })
                    .WithStorageQuota(11.GB())
                    .EnableMarketplace(geth, contracts, m => m
                        .WithInitial(10.Eth(), hostInitialBalance)
                        .AsStorageNode()
                        .AsValidator()));

                AssertBalance(contracts, host, Is.EqualTo(hostInitialBalance));

                var availability = new StorageAvailability(
                    totalSpace: 10.GB(),
                    maxDuration: TimeSpan.FromMinutes(30),
                    minPriceForTotalSpace: 1.TestTokens(),
                    maxCollateral: 20.TestTokens()
                );
                host.Marketplace.MakeStorageAvailable(availability);
            }

            var testFile = GenerateTestFile(fileSize);

            var client = AddCodex(s => s
                .WithName("Client")
                .EnableMarketplace(geth, contracts, m => m
                    .WithInitial(10.Eth(), clientInitialBalance)));

            AssertBalance(contracts, client, Is.EqualTo(clientInitialBalance));

            var contentId = client.UploadFile(testFile);

            var purchase = new StoragePurchaseRequest(contentId)
            {
                PricePerSlotPerSecond = 2.TestTokens(),
                RequiredCollateral = 10.TestTokens(),
                MinRequiredNumberOfNodes = 5,
                NodeFailureTolerance = 2,
                ProofProbability = 5,
                Duration = TimeSpan.FromMinutes(5),
                Expiry = TimeSpan.FromMinutes(4)
            };

            var purchaseContract = client.Marketplace.RequestStorage(purchase);

            WaitForAllSlotFilledEvents(contracts, purchase);

            purchaseContract.WaitForStorageContractStarted();

            //AssertBalance(contracts, host, Is.LessThan(hostInitialBalance), "Collateral was not placed.");

            var request = GetOnChainStorageRequest(contracts);
            AssertStorageRequest(request, purchase, contracts, client);
            //AssertSlotFilledEvents(contracts, purchase, request, host);
            //AssertContractSlot(contracts, request, 0, host);

            purchaseContract.WaitForStorageContractFinished();

            //AssertBalance(contracts, host, Is.GreaterThan(hostInitialBalance), "Seller was not paid for storage.");
            AssertBalance(contracts, client, Is.LessThan(clientInitialBalance), "Buyer was not charged for storage.");
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Finished));
        }

        private void WaitForAllSlotFilledEvents(ICodexContracts contracts, StoragePurchaseRequest purchase)
        {
            Time.Retry(() =>
            {
                var slotFilledEvents = contracts.GetSlotFilledEvents(GetTestRunTimeRange());

                Log($"SlotFilledEvents: {slotFilledEvents.Length} - NumSlots: {purchase.MinRequiredNumberOfNodes}");

                if (slotFilledEvents.Length != purchase.MinRequiredNumberOfNodes) throw new Exception();
            }, Convert.ToInt32(purchase.Duration.TotalSeconds / 5) + 10, TimeSpan.FromSeconds(5), "Checking SlotFilled events");
        }

        private void AssertSlotFilledEvents(ICodexContracts contracts, StoragePurchaseRequest purchase, Request request, ICodexNode seller)
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

        private void AssertStorageRequest(Request request, StoragePurchaseRequest purchase, ICodexContracts contracts, ICodexNode buyer)
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
