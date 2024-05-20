using CodexContractsPlugin;
using CodexDiscordBotPlugin;
using CodexPlugin;
using GethPlugin;
using KubernetesWorkflow.Types;
using NUnit.Framework;
using Utils;

namespace CodexTests.UtilityTests
{
    [TestFixture]
    public class DiscordBotTests : AutoBootstrapDistTest
    {
        private readonly TestToken hostInitialBalance = 3000.TestTokens();
        private readonly TestToken clientInitialBalance = 1000000000.TestTokens();
        private readonly ByteSize fileSize = 11.MB();

        [Test]
        public void BotRewardTest()
        {
            var clientAccount = EthAccount.GenerateNew();

            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);
            var gethInfo = CreateGethInfo(geth, contracts);

            var botContainer = StartDiscordBot(gethInfo);

            var hostAccount = EthAccount.GenerateNew();
            StartHosts(hostAccount, geth, contracts);

            StartRewarderBot(gethInfo, botContainer);

            var client = StartClient(geth, contracts, clientAccount);

            var purchaseContract = ClientPurchasesStorage(client);

            //purchaseContract.WaitForStorageContractStarted();
            //purchaseContract.WaitForStorageContractFinished();
            Thread.Sleep(TimeSpan.FromMinutes(5));

            var botLog = Ci.ExecuteContainerCommand(botContainer, "cat", "/app/datapath/logs/discordbot.log");
            var aaaa = 0;
        }

        private StoragePurchaseContract ClientPurchasesStorage(ICodexNode client)
        {
            var testFile = GenerateTestFile(fileSize);
            var contentId = client.UploadFile(testFile);
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

            return client.Marketplace.RequestStorage(purchase);
        }

        private ICodexNode StartClient(IGethNode geth, ICodexContracts contracts, EthAccount clientAccount)
        {
            return StartCodex(s => s
                .WithName("Client")
                .EnableMarketplace(geth, contracts, m => m
                    .WithAccount(clientAccount)
                    .WithInitial(10.Eth(), clientInitialBalance)));
        }

        private void StartRewarderBot(DiscordBotGethInfo gethInfo, RunningContainer botContainer)
        {
            Ci.DeployRewarderBot(new RewarderBotStartupConfig(
                discordBotHost: botContainer.GetInternalAddress(DiscordBotContainerRecipe.RewardsPort).Host,
                discordBotPort: botContainer.GetInternalAddress(DiscordBotContainerRecipe.RewardsPort).Port,
                intervalMinutes: "10",
                historyStartUtc: DateTime.UtcNow,
                gethInfo: gethInfo,
                dataPath: null
            ));
        }

        private DiscordBotGethInfo CreateGethInfo(IGethNode geth, ICodexContracts contracts)
        {
            return new DiscordBotGethInfo(
                host: geth.Container.GetInternalAddress(GethContainerRecipe.HttpPortTag).Host,
                port: geth.Container.GetInternalAddress(GethContainerRecipe.HttpPortTag).Port,
                privKey: geth.StartResult.Account.PrivateKey,
                marketplaceAddress: contracts.Deployment.MarketplaceAddress,
                tokenAddress: contracts.Deployment.TokenAddress,
                abi: contracts.Deployment.Abi
            );
        }

        private RunningContainer StartDiscordBot(DiscordBotGethInfo gethInfo)
        {
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
            return bot.Containers.Single();
        }

        private void StartHosts(EthAccount hostAccount, IGethNode geth, ICodexContracts contracts)
        {
            var numberOfHosts = 5;
            var hosts = StartCodex(numberOfHosts, s => s
                .WithName("Host")
                .WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Error, CodexLogLevel.Error, CodexLogLevel.Warn)
                {
                    ContractClock = CodexLogLevel.Trace,
                })
                .WithStorageQuota(11.GB())
                .EnableMarketplace(geth, contracts, m => m
                    .WithAccount(hostAccount)
                    .WithInitial(10.Eth(), hostInitialBalance)
                    .AsStorageNode()
                    .AsValidator()));

            var availability = new StorageAvailability(
                totalSpace: 10.GB(),
                maxDuration: TimeSpan.FromMinutes(30),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens()
            );

            foreach (var host in hosts)
            {
                host.Marketplace.MakeStorageAvailable(availability);
            }
        }
    }
}
