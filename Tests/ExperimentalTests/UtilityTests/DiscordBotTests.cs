﻿using CodexClient;
using CodexContractsPlugin;
using CodexDiscordBotPlugin;
using CodexPlugin;
using CodexTests;
using Core;
using DiscordRewards;
using DistTestCore;
using GethPlugin;
using KubernetesWorkflow.Types;
using Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using Utils;

namespace ExperimentalTests.UtilityTests
{
    [TestFixture]
    public class DiscordBotTests : AutoBootstrapDistTest
    {
        private readonly RewardRepo repo = new RewardRepo();
        private readonly TestToken hostInitialBalance = 3000000.TstWei();
        private readonly TestToken clientInitialBalance = 1000000000.TstWei();
        private readonly EthAccount clientAccount = EthAccountGenerator.GenerateNew();
        private readonly List<EthAccount> hostAccounts = new List<EthAccount>();
        private readonly List<ulong> rewardsSeen = new List<ulong>();
        private readonly TimeSpan rewarderInterval = TimeSpan.FromMinutes(1);
        private readonly List<ChainEventMessage> receivedEvents = new List<ChainEventMessage>();

        [Test]
        [DontDownloadLogs]
        [Ignore("Used to debug testnet bots.")]
        public void BotRewardTest()
        {
            var geth = StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);
            var gethInfo = CreateGethInfo(geth, contracts);

            var botContainer = StartDiscordBot(gethInfo);
            var rewarderContainer = StartRewarderBot(gethInfo, botContainer);

            StartHosts(geth, contracts);
            var client = StartClient(geth, contracts);

            var apiCalls = new RewardApiCalls(GetTestLog(), Ci, botContainer);
            apiCalls.Start(OnCommand);

            var purchaseContract = ClientPurchasesStorage(client);
            purchaseContract.WaitForStorageContractStarted();
            purchaseContract.WaitForStorageContractFinished();

            // todo: removed from codexclient:
            //contracts.WaitUntilNextPeriod();
            //contracts.WaitUntilNextPeriod();

            //var blocks = 3;
            //Log($"Waiting {blocks} blocks for nodes to process payouts...");
            //Thread.Sleep(GethContainerRecipe.BlockInterval * blocks);

            Thread.Sleep(rewarderInterval * 3);

            apiCalls.Stop();

            AssertEventOccurance("Created as New.", 1);
            AssertEventOccurance("SlotFilled", Convert.ToInt32(GetNumberOfRequiredHosts()));
            AssertEventOccurance("Transit: New -> Started", 1);
            AssertEventOccurance("Transit: Started -> Finished", 1);

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

        private void AssertEventOccurance(string msg, int expectedCount)
        {
            Assert.That(receivedEvents.Count(e => e.Message.Contains(msg)), Is.EqualTo(expectedCount),
                $"Event '{msg}' did not occure correct number of times.");
        }

        private void OnCommand(string timestamp, GiveRewardsCommand call)
        {
            Log($"<API call {timestamp}>");
            foreach (var e in call.EventsOverview)
            {
                Assert.That(receivedEvents.All(r => r.BlockNumber < e.BlockNumber), "Received event out of order.");
            }

            receivedEvents.AddRange(call.EventsOverview);
            foreach (var e in call.EventsOverview)
            {
                Log("\tEvent: " + e);
            }
            foreach (var r in call.Rewards)
            {
                var reward = repo.Rewards.Single(a => a.RoleId == r.RewardId);
                if (r.UserAddresses.Any()) rewardsSeen.Add(reward.RoleId);
                foreach (var address in r.UserAddresses)
                {
                    var user = IdentifyAccount(address);
                    Log("\tReward: " + user + ": " + reward.Message);
                }
            }
            Log($"</API call>");
        }

        private IStoragePurchaseContract ClientPurchasesStorage(ICodexNode client)
        {
            var testFile = GenerateTestFile(GetMinFileSize());
            var contentId = client.UploadFile(testFile);
            var purchase = new StoragePurchaseRequest(contentId)
            {
                PricePerBytePerSecond = 2.TstWei(),
                CollateralPerByte = 10.TstWei(),
                MinRequiredNumberOfNodes = GetNumberOfRequiredHosts(),
                NodeFailureTolerance = 2,
                ProofProbability = 5,
                Duration = GetMinRequiredRequestDuration(),
                Expiry = GetMinRequiredRequestDuration() - TimeSpan.FromMinutes(1)
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

            var availability = new CreateStorageAvailability(
                totalSpace: Mult(GetMinFileSize(), GetNumberOfLiveHosts()),
                maxDuration: TimeSpan.FromMinutes(30),
                minPricePerBytePerSecond: 1.TstWei(),
                totalCollateral: hostInitialBalance
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

            var minFileSize = (minSlotSize + 1024) * minNumHosts;
            return new ByteSize(Convert.ToInt64(minFileSize));
        }

        private uint GetNumberOfRequiredHosts()
        {
            return Convert.ToUInt32(repo.Rewards.Max(r => r.CheckConfig.MinNumberOfHosts));
        }

        private TimeSpan GetMinRequiredRequestDuration()
        {
            return repo.Rewards.Max(r => r.CheckConfig.MinDuration) + TimeSpan.FromSeconds(10);
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

            public RewardApiCalls(ILog log, CoreInterface ci, RunningContainer botContainer)
            {
                monitor = new ContainerFileMonitor(log, ci, botContainer, "/app/datapath/logs/discordbot.log");
            }

            public void Start(Action<string, GiveRewardsCommand> onCommand)
            {
                monitor.Start(line => ParseLine(line, onCommand));
            }

            public void Stop()
            {
                monitor.Stop();
            }

            private void ParseLine(string line, Action<string, GiveRewardsCommand> onCommand)
            {
                try
                {
                    var timestamp = line.Substring(0, 30);
                    var json = line.Substring(31);

                    var cmd = JsonConvert.DeserializeObject<GiveRewardsCommand>(json);
                    if (cmd != null)
                    {
                        onCommand(timestamp, cmd);
                    }
                }
                catch
                {
                }
            }
        }

        public class ContainerFileMonitor
        {
            private readonly ILog log;
            private readonly CoreInterface ci;
            private readonly RunningContainer botContainer;
            private readonly string filePath;
            private readonly CancellationTokenSource cts = new CancellationTokenSource();
            private readonly List<string> seenLines = new List<string>();
            private Task worker = Task.CompletedTask;
            private Action<string> onNewLine = c => { };

            public ContainerFileMonitor(ILog log, CoreInterface ci, RunningContainer botContainer, string filePath)
            {
                this.log = log;
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

            // did any container crash? that's why it repeats?


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
                    // log.Log("line: " + line);

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
