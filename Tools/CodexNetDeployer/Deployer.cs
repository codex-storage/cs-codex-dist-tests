using CodexContractsPlugin;
using CodexDiscordBotPlugin;
using CodexPlugin;
using Core;
using GethPlugin;
using KubernetesWorkflow.Types;
using Logging;
using MetricsPlugin;

namespace CodexNetDeployer
{
    public class Deployer
    {
        private readonly Configuration config;
        private readonly PeerConnectivityChecker peerConnectivityChecker;
        private readonly EntryPoint entryPoint;
        private readonly LocalCodexBuilder localCodexBuilder;

        public Deployer(Configuration config)
        {
            this.config = config;
            peerConnectivityChecker = new PeerConnectivityChecker();
            localCodexBuilder = new LocalCodexBuilder(new ConsoleLog(), config.CodexLocalRepoPath);

            ProjectPlugin.Load<CodexPlugin.CodexPlugin>();
            ProjectPlugin.Load<CodexContractsPlugin.CodexContractsPlugin>();
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<MetricsPlugin.MetricsPlugin>();
            ProjectPlugin.Load<CodexDiscordBotPlugin.CodexDiscordBotPlugin>();
            entryPoint = CreateEntryPoint(new NullLog());
        }

        public void AnnouncePlugins()
        {
            var ep = CreateEntryPoint(new ConsoleLog());

            localCodexBuilder.Intialize();

            Log("Using plugins:" + Environment.NewLine);
            var metadata = ep.GetPluginMetadata();
            var longestKey = metadata.Keys.Max(k => k.Length);
            foreach (var entry in metadata)
            {
                Console.Write(entry.Key);
                Console.CursorLeft = longestKey + 5;
                Console.WriteLine($"= {entry.Value}");
            }
            Log("");
        }

        public CodexDeployment Deploy()
        {
            localCodexBuilder.Build();

            Log("Initializing...");
            var startUtc = DateTime.UtcNow;
            var ci = entryPoint.CreateInterface();

            Log("Deploying Geth instance...");
            var gethDeployment = DeployGeth(ci);
            var gethNode = ci.WrapGethDeployment(gethDeployment);

            Log("Geth started. Deploying Codex contracts...");
            var contractsDeployment = ci.DeployCodexContracts(gethNode);
            var contracts = ci.WrapCodexContractsDeployment(gethNode, contractsDeployment);
            Log("Codex contracts deployed.");

            Log("Starting Codex nodes...");
            var codexStarter = new CodexNodeStarter(config, ci, gethNode, contracts, config.NumberOfValidators!.Value);
            var startResults = new List<CodexNodeStartResult>();
            for (var i = 0; i < config.NumberOfCodexNodes; i++)
            {
                var result = codexStarter.Start(i);
                if (result != null) startResults.Add(result);
            }

            Log("Codex nodes started.");
            var metricsService = StartMetricsService(ci, startResults);

            CheckPeerConnectivity(startResults);
            CheckContainerRestarts(startResults);

            var codexInstances = CreateCodexInstances(startResults);

            var discordBotContainer = DeployDiscordBot(ci, gethDeployment, contractsDeployment);

            return new CodexDeployment(codexInstances, gethDeployment, contractsDeployment, metricsService, discordBotContainer, CreateMetadata(startUtc));
        }

        private EntryPoint CreateEntryPoint(ILog log)
        {
            var kubeConfig = GetKubeConfig(config.KubeConfigFile);

            var configuration = new KubernetesWorkflow.Configuration(
                kubeConfig,
                operationTimeout: TimeSpan.FromMinutes(10),
                retryDelay: TimeSpan.FromSeconds(10),
                kubernetesNamespace: config.KubeNamespace);

            var result = new EntryPoint(log, configuration, string.Empty, new FastHttpTimeSet());
            configuration.Hooks = new K8sHook(config.TestsTypePodLabel, result.GetPluginMetadata());

            return result;
        }

        private GethDeployment DeployGeth(CoreInterface ci)
        {
            return ci.DeployGeth(s =>
            {
                s.IsMiner();
                s.WithName("geth");

                if (config.IsPublicTestNet)
                {
                    s.AsPublicTestNet(new GethTestNetConfig(
                        discoveryPort: config.PublicGethDiscPort,
                        listenPort: config.PublicGethListenPort
                    ));
                }
            });
        }

        private RunningContainers? DeployDiscordBot(CoreInterface ci, GethDeployment gethDeployment, CodexContractsDeployment contractsDeployment)
        {
            if (!config.DeployDiscordBot) return null;
            Log("Deploying Discord bot...");

            var addr = gethDeployment.Container.GetInternalAddress(GethContainerRecipe.HttpPortTag);
            var info = new DiscordBotGethInfo(
                host: addr.Host,
                port: addr.Port,
                privKey: gethDeployment.Account.PrivateKey,
                marketplaceAddress: contractsDeployment.MarketplaceAddress,
                tokenAddress: contractsDeployment.TokenAddress,
                abi: contractsDeployment.Abi
            );

            var rc = ci.DeployCodexDiscordBot(new DiscordBotStartupConfig(
                name: "discordbot-" + config.DeploymentName,
                token: config.DiscordBotToken,
                serverName: config.DiscordBotServerName,
                adminRoleName: config.DiscordBotAdminRoleName,
                adminChannelName: config.DiscordBotAdminChannelName,
                kubeNamespace: config.KubeNamespace,
                gethInfo: info,
                rewardChannelName: config.DiscordBotRewardChannelName)
            {
                DataPath = config.DiscordBotDataPath
            });

            Log("Discord bot deployed.");
            return rc;
        }

        private RunningContainers? StartMetricsService(CoreInterface ci, List<CodexNodeStartResult> startResults)
        {
            if (!config.MetricsScraper || !startResults.Any()) return null;

            Log("Starting metrics service...");

            var runningContainer = ci.DeployMetricsCollector(startResults.Select(r => r.CodexNode).ToArray());

            Log("Metrics service started.");

            return runningContainer;
        }

        private CodexInstance[] CreateCodexInstances(List<CodexNodeStartResult> startResults)
        {
            // When freshly started, the Codex nodes are announcing themselves by an incorrect IP address.
            // Only after fully initialized do they update to the provided NAT address.
            // Therefore, we wait:
            Thread.Sleep(TimeSpan.FromSeconds(5));

            return startResults.Select(r => CreateCodexInstance(r.CodexNode)).ToArray();
        }

        private CodexInstance CreateCodexInstance(ICodexNode node)
        {
            return new CodexInstance(node.Container.RunningContainers, node.GetDebugInfo());
        }

        private string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }

        private void CheckPeerConnectivity(List<CodexNodeStartResult> codexContainers)
        {
            if (!config.CheckPeerConnection || !codexContainers.Any()) return;

            Log("Starting peer connectivity check for deployed nodes...");
            peerConnectivityChecker.CheckConnectivity(codexContainers);
            Log("Check passed.");
        }

        private void CheckContainerRestarts(List<CodexNodeStartResult> startResults)
        {
            var crashes = new List<RunningContainer>();
            Log("Starting container crash check...");
            foreach (var startResult in startResults)
            {
                var watcher = startResult.CodexNode.CrashWatcher;
                if (watcher == null) throw new Exception("Expected each CodexNode container to be created with a crash-watcher.");
                if (watcher.HasContainerCrashed()) crashes.Add(startResult.CodexNode.Container);
            }

            if (!crashes.Any())
            {
                Log("Check passed.");
            }
            else
            {
                Log($"Check failed. The following containers have crashed: {string.Join(",", crashes.Select(c => c.Name))}");
                throw new Exception("Deployment failed: One or more containers crashed.");
            }
        }

        private DeploymentMetadata CreateMetadata(DateTime startUtc)
        {
            return new DeploymentMetadata(
                name: config.DeploymentName,
                startUtc: startUtc,
                finishedUtc: DateTime.UtcNow,
                kubeNamespace: config.KubeNamespace,
                numberOfCodexNodes: config.NumberOfCodexNodes!.Value,
                numberOfValidators: config.NumberOfValidators!.Value,
                storageQuotaMB: config.StorageQuota!.Value,
                codexLogLevel: config.CodexLogLevel,
                initialTestTokens: config.InitialTestTokens,
                minPrice: config.MinPrice,
                maxCollateral: config.MaxCollateral,
                maxDuration: config.MaxDuration,
                blockTTL: config.BlockTTL,
                blockMI: config.BlockMI,
                blockMN: config.BlockMN);
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }

    public class FastHttpTimeSet : ITimeSet
    {
        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromSeconds(2);
        }

        public int HttpMaxNumberOfRetries()
        {
            return 2;
        }

        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromSeconds(10);
        }

        public TimeSpan K8sOperationTimeout()
        {
            return TimeSpan.FromMinutes(10);
        }

        public TimeSpan WaitForK8sServiceDelay()
        {
            return TimeSpan.FromSeconds(30);
        }
    }
}
