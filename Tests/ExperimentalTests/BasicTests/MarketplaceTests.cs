using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using FileUtils;
using GethPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class MarketplaceTests : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        [CreateTranscript(nameof(MarketplaceTests), includeBlockReceivedEvents: false)]
        public void MarketplaceExample(
            [Values(64)] int numBlocks,
            [Values(0)] int plusSizeKb,
            [Values(0)] int plusSizeBytes
        )
        {
            var hostInitialBalance = 234.TstWei();
            var clientInitialBalance = 100000.TstWei();
            var fileSize = new ByteSize(
                numBlocks * (64 * 1024) +
                plusSizeKb * 1024 +
                plusSizeBytes
            );

            var geth = StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);
            
            var numberOfHosts = 5;
            var hosts = StartCodex(numberOfHosts, s => s
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

            foreach (var host in hosts)
            {
                AssertBalance(contracts, host, Is.EqualTo(hostInitialBalance));

                var availability = new StorageAvailability(
                    totalSpace: 10.GB(),
                    maxDuration: TimeSpan.FromMinutes(30),
                    minPricePerBytePerSecond: 1.TstWei(),
                    totalCollateral: 20.TstWei()
                );
                host.Marketplace.MakeStorageAvailable(availability);
            }

            var testFile = CreateFile(fileSize);

            var client = StartCodex(s => s
                .WithName("Client")
                .EnableMarketplace(geth, contracts, m => m
                    .WithInitial(10.Eth(), clientInitialBalance)));

            AssertBalance(contracts, client, Is.EqualTo(clientInitialBalance));

            var uploadCid = client.UploadFile(testFile);

            var purchase = new StoragePurchaseRequest(uploadCid)
            {
                PricePerBytePerSecond = 2.TstWei(),
                CollateralPerByte = 10.TstWei(),
                MinRequiredNumberOfNodes = 5,
                NodeFailureTolerance = 2,
                ProofProbability = 5,
                Duration = TimeSpan.FromMinutes(6),
                Expiry = TimeSpan.FromMinutes(5)
            };

            var purchaseContract = client.Marketplace.RequestStorage(purchase);
            
            var contractCid = purchaseContract.ContentId;
            Assert.That(uploadCid.Id, Is.Not.EqualTo(contractCid.Id));

            // Download both from client.
            testFile.AssertIsEqual(client.DownloadContent(uploadCid));
            testFile.AssertIsEqual(client.DownloadContent(contractCid));

            // Download both from another node.
            var downloader = StartCodex(s => s.WithName("Downloader"));
            testFile.AssertIsEqual(downloader.DownloadContent(uploadCid));
            testFile.AssertIsEqual(downloader.DownloadContent(contractCid));

            WaitForAllSlotFilledEvents(contracts, purchase, geth);

            purchaseContract.WaitForStorageContractStarted();

            var availabilities = hosts.Select(h => h.Marketplace.GetAvailabilities()).ToArray();
            if (availabilities.All(h => h.All(a => a.FreeSpace.SizeInBytes == a.TotalSpace.SizeInBytes)))
            {
                Assert.Fail("Host availabilities were not used.");
            }

            var request = GetOnChainStorageRequest(contracts, geth);
            AssertStorageRequest(request, purchase, contracts, client);
            AssertContractSlot(contracts, request, 0);

            purchaseContract.WaitForStorageContractFinished(contracts);

            AssertBalance(contracts, client, Is.LessThan(clientInitialBalance), "Buyer was not charged for storage.");
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Finished));
        }

        private TrackedFile CreateFile(ByteSize fileSize)
        {
            var segmentSize = new ByteSize(fileSize.SizeInBytes / 4);

            return GenerateTestFile(o => o
                .Random(segmentSize)
                .ByteRepeat(new byte[] { 0xaa }, segmentSize)
                .Random(segmentSize)
                .ByteRepeat(new byte[] { 0xee }, segmentSize)
            );
        }

        private void WaitForAllSlotFilledEvents(ICodexContracts contracts, StoragePurchaseRequest purchase, IGethNode geth)
        {
            Time.Retry(() =>
            {
                var events = contracts.GetEvents(GetTestRunTimeRange());
                var slotFilledEvents = events.GetSlotFilledEvents();

                var msg = $"SlotFilledEvents: {slotFilledEvents.Length} - NumSlots: {purchase.MinRequiredNumberOfNodes}";
                Debug(msg);
                if (slotFilledEvents.Length != purchase.MinRequiredNumberOfNodes) throw new Exception(msg);
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
            var events = contracts.GetEvents(GetTestRunTimeRange());
            var requests = events.GetStorageRequests();
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
