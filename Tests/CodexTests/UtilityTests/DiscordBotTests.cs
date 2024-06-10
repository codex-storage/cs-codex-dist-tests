using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using CodexDiscordBotPlugin;
using CodexPlugin;
using Core;
using DiscordRewards;
using GethPlugin;
using KubernetesWorkflow.Types;
using Newtonsoft.Json;
using NUnit.Framework;
using Utils;

namespace CodexTests.UtilityTests
{
    [TestFixture]
    public class DiscordBotTests : AutoBootstrapDistTest
    {
        private readonly RewardRepo repo = new RewardRepo();
        private readonly TestToken hostInitialBalance = 3000000.TstWei();
        private readonly TestToken clientInitialBalance = 1000000000.TstWei();
        private readonly EthAccount clientAccount = EthAccount.GenerateNew();
        private readonly List<EthAccount> hostAccounts = new List<EthAccount>();
        private readonly List<ulong> rewardsSeen = new List<ulong>();
        private readonly TimeSpan rewarderInterval = TimeSpan.FromMinutes(1);

        [Test]
        public void BotRewardTest()
        {
            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);
            var gethInfo = CreateGethInfo(geth, contracts);

            var botContainer = StartDiscordBot(gethInfo);

            StartHosts(geth, contracts);

            var rewarderContainer = StartRewarderBot(gethInfo, botContainer);

            var client = StartClient(geth, contracts);

            var events = ChainEvents.FromTimeRange(contracts, GetTestRunTimeRange());
            var chainState = ChainState.FromEvents(
                GetTestLog(),
                events,
                new DoNothingChainEventHandler());

            var apiCalls = new RewardApiCalls(Ci, botContainer);
            apiCalls.Start(OnCommand);

            var purchaseContract = ClientPurchasesStorage(client);
            chainState.Update(contracts);
            Assert.That(chainState.Requests.Length, Is.EqualTo(1));

            purchaseContract.WaitForStorageContractStarted();
            chainState.Update(contracts);

            purchaseContract.WaitForStorageContractFinished();

            Thread.Sleep(rewarderInterval * 2);
            
            apiCalls.Stop();
            chainState.Update(contracts);

            foreach (var r in repo.Rewards)
            {
                var seen = rewardsSeen.Any(s => r.RoleId == s);

                Log($"{Lookup(r.RoleId)} = {seen}");
            }

            Assert.That(repo.Rewards.All(r => rewardsSeen.Contains(r.RoleId)));
        }

        private string Lookup(ulong rewardId)
        {
            var reward = repo.Rewards.Single(r => r.RoleId == rewardId);
            return $"({rewardId})'{reward.Message}'";
        }

        private void OnCommand(GiveRewardsCommand call)
        {
            if (call.Averages.Any()) Log($"API call: {call.Averages.Length} average.");
            if (call.EventsOverview.Any()) Log($"API call: {call.EventsOverview.Length} events.");
            foreach (var r in call.Rewards)
            {
                var reward = repo.Rewards.Single(a => a.RoleId == r.RewardId);
                if (r.UserAddresses.Any()) rewardsSeen.Add(reward.RoleId);
                foreach (var address in r.UserAddresses)
                {
                    var user = IdentifyAccount(address);
                    Log("API call: " + user + ": " + reward.Message);
                }
            }
        }

        private IStoragePurchaseContract ClientPurchasesStorage(ICodexNode client)
        {
            var testFile = GenerateTestFile(GetMinFileSize());
            var contentId = client.UploadFile(testFile);
            var purchase = new StoragePurchaseRequest(contentId)
            {
                PricePerSlotPerSecond = 2.TstWei(),
                RequiredCollateral = 10.TstWei(),
                MinRequiredNumberOfNodes = GetNumberOfRequiredHosts(),
                NodeFailureTolerance = 2,
                ProofProbability = 5,
                Duration = TimeSpan.FromMinutes(6),
                Expiry = TimeSpan.FromMinutes(5)
            };

            return client.Marketplace.RequestStorage(purchase);
        }

        private ICodexNode StartClient(IGethNode geth, ICodexContracts contracts)
        {
            var node = StartCodex(s => s
                .WithName("Client")
                .EnableMarketplace(geth, contracts, m => m
                    .WithAccount(clientAccount)
                    .WithInitial(10.Eth(), clientInitialBalance)));

            Log($"Client {node.EthAccount.EthAddress}");
            return node;
        }

        private RunningPod StartRewarderBot(DiscordBotGethInfo gethInfo, RunningContainer botContainer)
        {
            return Ci.DeployRewarderBot(new RewarderBotStartupConfig(
                name: "rewarder-bot",
                discordBotHost: botContainer.GetInternalAddress(DiscordBotContainerRecipe.RewardsPort).Host,
                discordBotPort: botContainer.GetInternalAddress(DiscordBotContainerRecipe.RewardsPort).Port,
                intervalMinutes: Convert.ToInt32(Math.Round(rewarderInterval.TotalMinutes)),
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
                name: "discord-bot",
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

        private void StartHosts(IGethNode geth, ICodexContracts contracts)
        {
            var hosts = StartCodex(GetNumberOfLiveHosts(), s => s
                .WithName("Host")
                .WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Error, CodexLogLevel.Error, CodexLogLevel.Warn)
                {
                    ContractClock = CodexLogLevel.Trace,
                })
                .WithStorageQuota(Mult(GetMinFileSizePlus(50), GetNumberOfLiveHosts()))
                .EnableMarketplace(geth, contracts, m => m
                    .WithInitial(10.Eth(), hostInitialBalance)
                    .AsStorageNode()
                    .AsValidator()));

            var availability = new StorageAvailability(
                totalSpace: Mult(GetMinFileSize(), GetNumberOfLiveHosts()),
                maxDuration: TimeSpan.FromMinutes(30),
                minPriceForTotalSpace: 1.TstWei(),
                maxCollateral: hostInitialBalance
            );

            foreach (var host in hosts)
            {
                hostAccounts.Add(host.EthAccount);
                host.Marketplace.MakeStorageAvailable(availability);
            }
        }

        private int GetNumberOfLiveHosts()
        {
            return Convert.ToInt32(GetNumberOfRequiredHosts()) + 3;
        }

        private ByteSize Mult(ByteSize size, int mult)
        {
            return new ByteSize(size.SizeInBytes * mult);
        }

        private ByteSize GetMinFileSizePlus(int plusMb)
        {
            return new ByteSize(GetMinFileSize().SizeInBytes + plusMb.MB().SizeInBytes);
        }

        private ByteSize GetMinFileSize()
        {
            ulong minSlotSize = 0;
            ulong minNumHosts = 0;
            foreach (var r in repo.Rewards)
            {
                var s = Convert.ToUInt64(r.CheckConfig.MinSlotSize.SizeInBytes);
                var h = r.CheckConfig.MinNumberOfHosts;
                if (s > minSlotSize) minSlotSize = s;
                if (h > minNumHosts) minNumHosts = h;
            }

            var minFileSize = ((minSlotSize + 1024) * minNumHosts);
            return new ByteSize(Convert.ToInt64(minFileSize));
        }

        private uint GetNumberOfRequiredHosts()
        {
            return Convert.ToUInt32(repo.Rewards.Max(r => r.CheckConfig.MinNumberOfHosts));
        }

        private string IdentifyAccount(string address)
        {
            if (address == clientAccount.EthAddress.Address) return "Client";
            try
            {
                var index = hostAccounts.FindIndex(a => a.EthAddress.Address == address);
                return "Host" + index;
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        public class RewardApiCalls
        {
            private readonly ContainerFileMonitor monitor;
            private readonly Dictionary<string, GiveRewardsCommand> commands = new Dictionary<string, GiveRewardsCommand>();

            public RewardApiCalls(CoreInterface ci, RunningContainer botContainer)
            {
                monitor = new ContainerFileMonitor(ci, botContainer, "/app/datapath/logs/discordbot.log");
            }

            public void Start(Action<GiveRewardsCommand> onCommand)
            {
                monitor.Start(line => ParseLine(line, onCommand));
            }

            public void Stop()
            {
                monitor.Stop();
            }

            private void ParseLine(string line, Action<GiveRewardsCommand> onCommand)
            {
                try
                {
                    var timestamp = line.Substring(0, 30);
                    if (commands.ContainsKey(timestamp)) return;
                    var json = line.Substring(31);

                    var cmd = JsonConvert.DeserializeObject<GiveRewardsCommand>(json);
                    if (cmd != null)
                    {
                        commands.Add(timestamp, cmd);
                        onCommand(cmd);
                    }
                }
                catch
                {
                }
            }
        }

        public class ContainerFileMonitor
        {
            private readonly CoreInterface ci;
            private readonly RunningContainer botContainer;
            private readonly string filePath;
            private readonly CancellationTokenSource cts = new CancellationTokenSource();
            private readonly List<string> seenLines = new List<string>();
            private Task worker = Task.CompletedTask;
            private Action<string> onNewLine = c => { };

            public ContainerFileMonitor(CoreInterface ci, RunningContainer botContainer, string filePath)
            {
                this.ci = ci;
                this.botContainer = botContainer;
                this.filePath = filePath;
            }

            public void Start(Action<string> onNewLine)
            {
                this.onNewLine = onNewLine;
                worker = Task.Run(Worker);
            }

            public void Stop()
            {
                cts.Cancel();
                worker.Wait();
            }

            private void Worker()
            {
                while (!cts.IsCancellationRequested)
                {
                    Update();
                }
            }

            private void Update()
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                if (cts.IsCancellationRequested) return;

                var botLog = ci.ExecuteContainerCommand(botContainer, "cat", filePath);
                var lines = botLog.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (!seenLines.Contains(line))
                    {
                        seenLines.Add(line);
                        onNewLine(line);
                    }
                }
            }
        }
    }
}
