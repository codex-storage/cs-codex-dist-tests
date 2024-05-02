using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using GethPlugin;
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
            
            var numberOfHosts = 5;
            var hosts = AddCodex(numberOfHosts, s => s
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

            var expectedHostBalance = (numberOfHosts * hostInitialBalance.Amount).TestTokens();
            foreach (var host in hosts)
            {
                AssertBalance(contracts, host, Is.EqualTo(expectedHostBalance));

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

            WaitForAllSlotFilledEvents(contracts, purchase, geth);

            purchaseContract.WaitForStorageContractStarted();

            var request = GetOnChainStorageRequest(contracts, geth);
            AssertStorageRequest(request, purchase, contracts, client);
            AssertContractSlot(contracts, request, 0);

            purchaseContract.WaitForStorageContractFinished();

            AssertBalance(contracts, client, Is.LessThan(clientInitialBalance), "Buyer was not charged for storage.");
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Finished));
        }

        private void WaitForAllSlotFilledEvents(ICodexContracts contracts, StoragePurchaseRequest purchase, IGethNode geth)
        {
            Time.Retry(() =>
            {
                var blockRange = geth.ConvertTimeRangeToBlockRange(GetTestRunTimeRange());
                var slotFilledEvents = contracts.GetSlotFilledEvents(blockRange);

                Log($"SlotFilledEvents: {slotFilledEvents.Length} - NumSlots: {purchase.MinRequiredNumberOfNodes}");

                if (slotFilledEvents.Length != purchase.MinRequiredNumberOfNodes) throw new Exception();
            }, purchase.Expiry + TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), "Checking SlotFilled events");
        }

        private void AssertStorageRequest(Request request, StoragePurchaseRequest purchase, ICodexContracts contracts, ICodexNode buyer)
        {
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Started));
            Assert.That(request.ClientAddress, Is.EqualTo(buyer.EthAddress));
            Assert.That(request.Ask.Slots, Is.EqualTo(purchase.MinRequiredNumberOfNodes));
        }

        private Request GetOnChainStorageRequest(ICodexContracts contracts, IGethNode geth)
        {
            var requests = contracts.GetStorageRequests(geth.ConvertTimeRangeToBlockRange(GetTestRunTimeRange()));
            Assert.That(requests.Length, Is.EqualTo(1));
            return requests.Single();
        }

        private void AssertContractSlot(ICodexContracts contracts, Request request, int contractSlotIndex)
        {
            var slotHost = contracts.GetSlotHost(request, contractSlotIndex);
            Assert.That(slotHost?.Address, Is.Not.Null);
        }
    }
}
