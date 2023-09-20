using CodexContractsPlugin;
using CodexPlugin;
using Core;
using GethPlugin;
using KubernetesWorkflow;
using Logging;
using MetricsPlugin;

namespace CodexNetDeployer
{
    public class Deployer
    {
        private readonly Configuration config;
        private readonly PeerConnectivityChecker peerConnectivityChecker;
        private readonly EntryPoint entryPoint;

        public Deployer(Configuration config)
        {
            this.config = config;
            peerConnectivityChecker = new PeerConnectivityChecker();

            ProjectPlugin.Load<CodexPlugin.CodexPlugin>();
            ProjectPlugin.Load<CodexContractsPlugin.CodexContractsPlugin>();
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<MetricsPlugin.MetricsPlugin>();
            entryPoint = CreateEntryPoint(new NullLog());
        }

        public void AnnouncePlugins()
        {
            var ep = CreateEntryPoint(new ConsoleLog());

            Log("Using plugins:" + Environment.NewLine);
            var metadata = ep.GetPluginMetadata();
            foreach (var entry in metadata)
            {
                Log($"{entry.Key}   =   {entry.Value}");
            }
            Log("");
        }

        public CodexDeployment Deploy()
        {
            Log("Initializing...");
            var ci = entryPoint.CreateInterface();

            Log("Deploying Geth instance...");
            var gethDeployment = ci.DeployGeth(s => s.IsMiner());
            var gethNode = ci.WrapGethDeployment(gethDeployment);

            Log("Geth started. Deploying Codex contracts...");
            var contractsDeployment = ci.DeployCodexContracts(gethNode);
            var contracts = ci.WrapCodexContractsDeployment(contractsDeployment);
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

            var codexContainers = startResults.Select(s => s.CodexNode.Container).ToArray();
            return new CodexDeployment(codexContainers, gethDeployment, metricsService, CreateMetadata());
        }

        private EntryPoint CreateEntryPoint(ILog log)
        {
            var kubeConfig = GetKubeConfig(config.KubeConfigFile);

            var configuration = new KubernetesWorkflow.Configuration(
                kubeConfig,
                operationTimeout: TimeSpan.FromSeconds(30),
                retryDelay: TimeSpan.FromSeconds(10),
                kubernetesNamespace: config.KubeNamespace);

            return new EntryPoint(log, configuration, string.Empty);
        }

        private RunningContainer? StartMetricsService(CoreInterface ci, List<CodexNodeStartResult> startResults)
        {
            if (!config.Metrics) return null;

            Log("Starting metrics service...");

            var runningContainer = ci.DeployMetricsCollector(startResults.Select(r => r.CodexNode).ToArray());

            Log("Metrics service started.");

            return runningContainer;
        }

        private string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }

        private void CheckPeerConnectivity(List<CodexNodeStartResult> codexContainers)
        {
            if (!config.CheckPeerConnection) return;

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

        private DeploymentMetadata CreateMetadata()
        {
            return new DeploymentMetadata(
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
}
