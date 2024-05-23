using CodexContractsPlugin;
using CodexDiscordBotPlugin;
using CodexPlugin;
using Core;
using DiscordRewards;
using GethPlugin;
using KubernetesWorkflow.Types;
using Logging;
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
        private readonly EthAccount hostAccount = EthAccount.GenerateNew();

        [Test]
        public void BotRewardTest()
        {
            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);
            var gethInfo = CreateGethInfo(geth, contracts);

            var monitor = new ChainMonitor(contracts, geth, GetTestLog());
            monitor.Start();

            var botContainer = StartDiscordBot(gethInfo);

            StartHosts(geth, contracts);

            StartRewarderBot(gethInfo, botContainer);

            var client = StartClient(geth, contracts);

            var purchaseContract = ClientPurchasesStorage(client);

            var apiCalls = new RewardApiCalls(Ci, botContainer);
            apiCalls.Start(OnCommand);

            Thread.Sleep(TimeSpan.FromMinutes(10));

            apiCalls.Stop();
            monitor.Stop();
        }

        private void OnCommand(GiveRewardsCommand call)
        {
            var line = "";
            if (call.Averages.Any()) line += $"{call.Averages.Length} average. ";
            if (call.EventsOverview.Any()) line += $"{call.EventsOverview.Length} events. ";
            foreach (var r in call.Rewards)
            {
                var reward = repo.Rewards.Single(a => a.RoleId == r.RewardId);
                var isClient = r.UserAddresses.Any(a => a == clientAccount.EthAddress.Address);
                var isHost = r.UserAddresses.Any(a => a == hostAccount.EthAddress.Address);
                if (isHost && isClient) throw new Exception("what?");
                var name = isClient ? "Client" : "Host";

                line += name + " = " + reward.Message;
            }
            Log(line);
        }

        private StoragePurchaseContract ClientPurchasesStorage(ICodexNode client)
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

        private void StartHosts(IGethNode geth, ICodexContracts contracts)
        {
            var hosts = StartCodex(GetNumberOfLiveHosts(), s => s
                .WithName("Host")
                .WithLogLevel(CodexLogLevel.Trace, new CodexLogCustomTopics(CodexLogLevel.Error, CodexLogLevel.Error, CodexLogLevel.Warn)
                {
                    ContractClock = CodexLogLevel.Trace,
                })
                .WithStorageQuota(GetFileSizePlus(50))
                .EnableMarketplace(geth, contracts, m => m
                    .WithAccount(hostAccount)
                    .WithInitial(10.Eth(), hostInitialBalance)
                    .AsStorageNode()
                    .AsValidator()));

            var availability = new StorageAvailability(
                totalSpace: GetFileSizePlus(5),
                maxDuration: TimeSpan.FromMinutes(30),
                minPriceForTotalSpace: 1.TstWei(),
                maxCollateral: hostInitialBalance
            );

            foreach (var host in hosts)
            {
                host.Marketplace.MakeStorageAvailable(availability);
            }
        }

        private int GetNumberOfLiveHosts()
        {
            return Convert.ToInt32(GetNumberOfRequiredHosts()) + 3;
        }

        private ByteSize GetFileSizePlus(int plusMb)
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

            var minFileSize = (minSlotSize * minNumHosts) + 1024;
            return new ByteSize(Convert.ToInt64(minFileSize));
        }

        private uint GetNumberOfRequiredHosts()
        {
            return Convert.ToUInt32(repo.Rewards.Max(r => r.CheckConfig.MinNumberOfHosts));
        }

        public class RewardApiCalls
        {
            private readonly CoreInterface ci;
            private readonly RunningContainer botContainer;
            private readonly Dictionary<string, GiveRewardsCommand> commands = new Dictionary<string, GiveRewardsCommand>();
            private readonly CancellationTokenSource cts = new CancellationTokenSource();
            private Task worker = Task.CompletedTask;
            private Action<GiveRewardsCommand> onCommand = c => { };

            public RewardApiCalls(CoreInterface ci, RunningContainer botContainer)
            {
                this.ci = ci;
                this.botContainer = botContainer;
            }

            public void Start(Action<GiveRewardsCommand> onCommand)
            {
                this.onCommand = onCommand;
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

                var botLog = ci.ExecuteContainerCommand(botContainer, "cat", "/app/datapath/logs/discordbot.log");
                var lines = botLog.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    AddToCache(line);
                }
            }

            private void AddToCache(string line)
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

        public class ChainMonitor
        {
            private readonly ICodexContracts contracts;
            private readonly IGethNode geth;
            private readonly ILog log;
            private readonly CancellationTokenSource cts = new CancellationTokenSource();
            private Task worker = Task.CompletedTask;
            private DateTime last = DateTime.UtcNow;

            public ChainMonitor(ICodexContracts contracts, IGethNode geth, ILog log)
            {
                this.contracts = contracts;
                this.geth = geth;
                this.log = log;
            }

            public void Start()
            {
                last = DateTime.UtcNow;
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
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    if (cts.IsCancellationRequested) return;

                    Update();

                }
            }

            private void Update()
            {
                var start = last;
                var stop = DateTime.UtcNow;
                last = stop;

                var range = geth.ConvertTimeRangeToBlockRange(new TimeRange(start, stop));


                LogEvents(nameof(contracts.GetStorageRequests), contracts.GetStorageRequests, range);
                LogEvents(nameof(contracts.GetRequestFulfilledEvents), contracts.GetRequestFulfilledEvents, range);
                LogEvents(nameof(contracts.GetRequestCancelledEvents), contracts.GetRequestCancelledEvents, range);
                LogEvents(nameof(contracts.GetSlotFilledEvents), contracts.GetSlotFilledEvents, range);
                LogEvents(nameof(contracts.GetSlotFreedEvents), contracts.GetSlotFreedEvents, range);
            }

            private void LogEvents(string n, Func<BlockInterval, object> f, BlockInterval r)
            {
                var a = (object[])f(r);

                a.ToList().ForEach(request => log.Log(n + " - " + JsonConvert.SerializeObject(request)));
            }
        }
    }
}
