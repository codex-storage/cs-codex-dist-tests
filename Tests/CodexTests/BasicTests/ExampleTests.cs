using CodexContractsPlugin;
using CodexDiscordBotPlugin;
using CodexPlugin;
using DistTestCore;
using GethPlugin;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class ExampleTests : CodexDistTest
    {
        [Test]
        public void BotRewardTest()
        {
            var sellerInitialBalance = 234.TestTokens();
            var buyerInitialBalance = 1000.TestTokens();
            var fileSize = 10.MB();

            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);

            var seller = AddCodex(s => s
                .WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Error, CodexLogLevel.Error, CodexLogLevel.Warn))
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
                token: "MTE2NDEyNzk3MDU4NDE3NDU5Mw.GTpoV6.aDR7zxMNf7vDgMjKASJBQs-RtNP_lYJEY-OglI",
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

            var sellerAddress = seller.EthAddress;
            var buyerAddress = buyer.EthAddress;

            var i = 0;

            var contentId = buyer.UploadFile(testFile);
            var purchaseContract = buyer.Marketplace.RequestStorage(contentId,
                pricePerSlotPerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 1,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(1));

            purchaseContract.WaitForStorageContractStarted(fileSize);

            var requests = contracts.GetStorageRequests(GetTestRunTimeRange());
            Assert.That(requests.Length, Is.EqualTo(1));
            var request = requests.Single();
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Started));
            Assert.That(request.ClientAddress, Is.EqualTo(buyer.EthAddress));
            Assert.That(request.Ask.Slots, Is.EqualTo(1));

            AssertBalance(contracts, seller, Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            var requestFulfilledEvents = contracts.GetRequestFulfilledEvents(GetTestRunTimeRange());
            Assert.That(requestFulfilledEvents.Length, Is.EqualTo(1));
            CollectionAssert.AreEqual(request.RequestId, requestFulfilledEvents[0].RequestId);
            var filledSlotEvents = contracts.GetSlotFilledEvents(GetTestRunTimeRange());
            Assert.That(filledSlotEvents.Length, Is.EqualTo(1));
            var filledSlotEvent = filledSlotEvents.Single();
            Assert.That(filledSlotEvent.SlotIndex.IsZero);
            Assert.That(filledSlotEvent.RequestId.ToHex(), Is.EqualTo(request.RequestId.ToHex()));
            Assert.That(filledSlotEvent.Host, Is.EqualTo(seller.EthAddress));

            var slotHost = contracts.GetSlotHost(request, 0);
            Assert.That(slotHost, Is.EqualTo(seller.EthAddress));

            purchaseContract.WaitForStorageContractFinished();

            AssertBalance(contracts, seller, Is.GreaterThan(sellerInitialBalance), "Seller was not paid for storage.");
            AssertBalance(contracts, buyer, Is.LessThan(buyerInitialBalance), "Buyer was not charged for storage.");
            Assert.That(contracts.GetRequestState(request), Is.EqualTo(RequestState.Finished));

            var log = Ci.DownloadLog(seller);
            log.AssertLogContains("Received a request to store a slot!");
            log.AssertLogContains("Received proof challenge");
            log.AssertLogContains("Collecting input for proof");

            //CheckLogForErrors(seller, buyer);
        }

    }
}
