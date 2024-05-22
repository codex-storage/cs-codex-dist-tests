using CodexContractsPlugin;
using CodexDiscordBotPlugin;
using CodexPlugin;
using GethPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.UtilityTests
{
    [TestFixture]
    public class DiscordBotTests : AutoBootstrapDistTest
    {
        [Test]
        [Ignore("Used for debugging bots")]
        public void BotRewardTest()
        {
            var myAccount = EthAccount.GenerateNew();

            var sellerInitialBalance = 234.TstWei();
            var buyerInitialBalance = 100000.TstWei();
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
                intervalMinutes: "1",
                historyStartUtc: GetTestRunTimeRange().From - TimeSpan.FromMinutes(3),
                gethInfo: gethInfo,
                dataPath: null
            ));

            var numberOfHosts = 3;

            for (var i = 0; i < numberOfHosts; i++)
            {
                var seller = StartCodex(s => s
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
                    minPriceForTotalSpace: 1.TstWei(),
                    maxCollateral: 20.TstWei()
                );
                seller.Marketplace.MakeStorageAvailable(availability);
            }

            var testFile = GenerateTestFile(fileSize);

            var buyer = StartCodex(s => s
                .WithName("Buyer")
                .EnableMarketplace(geth, contracts, m => m
                    .WithAccount(myAccount)
                    .WithInitial(10.Eth(), buyerInitialBalance)));

            var contentId = buyer.UploadFile(testFile);

            var purchase = new StoragePurchaseRequest(contentId)
            {
                PricePerSlotPerSecond = 2.TstWei(),
                RequiredCollateral = 10.TstWei(),
                MinRequiredNumberOfNodes = 5,
                NodeFailureTolerance = 2,
                ProofProbability = 5,
                Duration = TimeSpan.FromMinutes(6),
                Expiry = TimeSpan.FromMinutes(5)
            };

            var purchaseContract = buyer.Marketplace.RequestStorage(purchase);

            purchaseContract.WaitForStorageContractStarted();

            purchaseContract.WaitForStorageContractFinished();
        }
    }
}
