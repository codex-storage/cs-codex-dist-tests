using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexDiscordBotPlugin;
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
        public void BotRewardTest()
        {
            var myAccount = EthAccount.GenerateNew();

            var sellerInitialBalance = 234.TestTokens();
            var buyerInitialBalance = 100000.TestTokens();
            var fileSize = 11.MB();

            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);

            // start bot and rewarder
            var gethInfo = new DiscordBotGethInfo(
                host: geth.Container.GetInternalAddress(GethContainerRecipe.HttpPortTag).Host,
                port: geth.Container.GetInternalAddress(GethContainerRecipe.HttpPortTag).Port,
                privKey: geth.StartResult.Account.PrivateKey,
                marketplaceAddress: contracts.Deployment.MarketplaceAddress,
                tokenAddress: contracts.Deployment.TokenAddress,
                abi: contracts.Deployment.Abi
            );
            var bot = Ci.DeployCodexDiscordBot(new DiscordBotStartupConfig(
                name: "bot",
                token: "aaa",
                serverName: "ThatBen's server",
                adminRoleName: "bottest-admins",
                adminChannelName: "admin-channel",
                rewardChannelName: "rewards-channel",
                kubeNamespace: "notneeded",
                gethInfo: gethInfo
            ));
            var botContainer = bot.Containers.Single();
            Ci.DeployRewarderBot(new RewarderBotStartupConfig(
                //discordBotHost: "http://" + botContainer.GetAddress(GetTestLog(), DiscordBotContainerRecipe.RewardsPort).Host,
                //discordBotPort: botContainer.GetAddress(GetTestLog(), DiscordBotContainerRecipe.RewardsPort).Port,
                discordBotHost: botContainer.GetInternalAddress(DiscordBotContainerRecipe.RewardsPort).Host,
                discordBotPort: botContainer.GetInternalAddress(DiscordBotContainerRecipe.RewardsPort).Port,
                interval: "60",
                historyStartUtc: DateTime.UtcNow.AddHours(-1),
                gethInfo: gethInfo,
                dataPath: null
            ));

            var numberOfHosts = 3;

            for (var i = 0; i < numberOfHosts; i++)
            {
                var seller = AddCodex(s => s
                    .WithName("Seller")
                    .WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Error, CodexLogLevel.Error, CodexLogLevel.Warn)
                    {
                        ContractClock = CodexLogLevel.Trace,
                    })
                    .WithStorageQuota(11.GB())
                    .EnableMarketplace(geth, contracts, m => m
                        .WithAccount(myAccount)
                        .WithInitial(10.Eth(), sellerInitialBalance)
                        .AsStorageNode()
                        .AsValidator()));

                var availability = new StorageAvailability(
                    totalSpace: 10.GB(),
                    maxDuration: TimeSpan.FromMinutes(30),
                    minPriceForTotalSpace: 1.TestTokens(),
                    maxCollateral: 20.TestTokens()
                );
                seller.Marketplace.MakeStorageAvailable(availability);
            }

            var testFile = GenerateTestFile(fileSize);

            var buyer = AddCodex(s => s
                .WithName("Buyer")
                .EnableMarketplace(geth, contracts, m => m
                    .WithAccount(myAccount)
                    .WithInitial(10.Eth(), buyerInitialBalance)));

            var contentId = buyer.UploadFile(testFile);

            var purchase = new StoragePurchaseRequest(contentId)
            {
                PricePerSlotPerSecond = 2.TestTokens(),
                RequiredCollateral = 10.TestTokens(),
                MinRequiredNumberOfNodes = 5,
                NodeFailureTolerance = 2,
                ProofProbability = 5,
                Duration = TimeSpan.FromMinutes(6),
                Expiry = TimeSpan.FromMinutes(5)
            };

            var purchaseContract = buyer.Marketplace.RequestStorage(purchase);

            purchaseContract.WaitForStorageContractStarted();

            //AssertBalance(contracts, seller, Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            //var blockRange = geth.ConvertTimeRangeToBlockRange(GetTestRunTimeRange());

            //var request = GetOnChainStorageRequest(contracts, blockRange);
            //AssertStorageRequest(request, purchase, contracts, buyer);
            //AssertSlotFilledEvents(contracts, purchase, request, seller, blockRange);
            //AssertContractSlot(contracts, request, 0, seller);

            purchaseContract.WaitForStorageContractFinished();

            var hold = 0;

            //AssertBalance(contracts, seller, Is.GreaterThan(sellerInitialBalance), "Seller was not paid for storage.");
            //AssertBalance(contracts, buyer, Is.LessThan(buyerInitialBalance), "Buyer was not charged for storage.");
            //Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Finished));
        }

        private void WaitForAllSlotFilledEvents(IGethNode gethNode, ICodexContracts contracts, StoragePurchaseRequest purchase)
        {
            Time.Retry(() =>
            {
                var blockRange = gethNode.ConvertTimeRangeToBlockRange(GetTestRunTimeRange());
                var slotFilledEvents = contracts.GetSlotFilledEvents(blockRange);

                Log($"SlotFilledEvents: {slotFilledEvents.Length} - NumSlots: {purchase.MinRequiredNumberOfNodes}");

                if (slotFilledEvents.Length != purchase.MinRequiredNumberOfNodes) throw new Exception();
            }, Convert.ToInt32(purchase.Duration.TotalSeconds / 5) + 10, TimeSpan.FromSeconds(5), "Checking SlotFilled events");
        }

        //private void AssertSlotFilledEvents(ICodexContracts contracts, StoragePurchaseRequest purchase, Request request, ICodexNode seller)
        //{
        //    // Expect 1 fulfilled event for the purchase.
        //    var requestFulfilledEvents = contracts.GetRequestFulfilledEvents(GetTestRunTimeRange());
        //    Assert.That(requestFulfilledEvents.Length, Is.EqualTo(1));
        //    CollectionAssert.AreEqual(request.RequestId, requestFulfilledEvents[0].RequestId);

        //    // Expect 1 filled-slot event for each slot in the purchase.
        //    var filledSlotEvents = contracts.GetSlotFilledEvents(GetTestRunTimeRange());
        //    Assert.That(filledSlotEvents.Length, Is.EqualTo(purchase.MinRequiredNumberOfNodes));
        //    for (var i = 0; i < purchase.MinRequiredNumberOfNodes; i++)
        //    {
        //        var filledSlotEvent = filledSlotEvents.Single(e => e.SlotIndex == i);
        //        Assert.That(filledSlotEvent.RequestId.ToHex(), Is.EqualTo(request.RequestId.ToHex()));
        //        Assert.That(filledSlotEvent.Host, Is.EqualTo(seller.EthAddress));
        //    }
        //}

        private void AssertStorageRequest(Request request, StoragePurchaseRequest purchase, ICodexContracts contracts, ICodexNode buyer)
        {
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Started));
            Assert.That(request.ClientAddress, Is.EqualTo(buyer.EthAddress));
            Assert.That(request.Ask.Slots, Is.EqualTo(purchase.MinRequiredNumberOfNodes));
        }

        //private Request GetOnChainStorageRequest(ICodexContracts contracts)
        //{
        //    var requests = contracts.GetStorageRequests(GetTestRunTimeRange());
        //    Assert.That(requests.Length, Is.EqualTo(1));
        //    return requests.Single();
        //}

        private void AssertContractSlot(ICodexContracts contracts, Request request, int contractSlotIndex, ICodexNode expectedSeller)
        {
            var slotHost = contracts.GetSlotHost(request, contractSlotIndex);
            Assert.That(slotHost, Is.EqualTo(expectedSeller.EthAddress));
        }
    }
}
