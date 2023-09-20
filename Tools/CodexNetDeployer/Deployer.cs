using DistTestCore;
using DistTestCore.Codex;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace CodexNetDeployer
{
    public class Deployer
    {
        private readonly Configuration config;
        private readonly DefaultTimeSet timeset;
        private readonly PeerConnectivityChecker peerConnectivityChecker;

        public Deployer(Configuration config)
        {
            this.config = config;
            timeset = new DefaultTimeSet();
            peerConnectivityChecker = new PeerConnectivityChecker();
        }

        public CodexDeployment Deploy()
        {
            Log("Initializing...");
            var lifecycle = CreateTestLifecycle();

            Log("Preparing configuration...");
            // We trick the Geth companion node into unlocking all of its accounts, by saying we want to start 999 codex nodes.
            var setup = new CodexSetup(999, config.CodexLogLevel);
            setup.WithStorageQuota(config.StorageQuota!.Value.MB()).EnableMarketplace(0.TestTokens());
            setup.MetricsMode = config.Metrics;

            Log("Creating Geth instance and deploying contracts...");
            var gethStarter = new GethStarter(lifecycle);
            var gethResults = gethStarter.BringOnlineMarketplaceFor(setup);

            Log("Geth started. Codex contracts deployed.");
            Log("Warning: It can take up to 45 minutes for the Geth node to finish unlocking all if its 1000 preconfigured accounts.");

            // It takes a second for the geth node to unlock a single account. Let's assume 3.
            // We can't start the codex nodes until their accounts are definitely unlocked. So
            // We wait:
            Thread.Sleep(TimeSpan.FromSeconds(3.0 * config.NumberOfCodexNodes!.Value));

            Log("Starting Codex nodes...");

            // Each node must have its own IP, so it needs it own pod. Start them 1 at a time.
            var codexStarter = new CodexNodeStarter(config, lifecycle, gethResults, config.NumberOfValidators!.Value);
            var startResults = new List<CodexNodeStartResult>();
            for (var i = 0; i < config.NumberOfCodexNodes; i++)
            {
                var result = codexStarter.Start(i);
                if (result != null) startResults.Add(result);
            }

            var (prometheusContainer, grafanaStartInfo) = StartMetricsService(lifecycle, setup, startResults.Select(r => r.Container));

            CheckPeerConnectivity(startResults);
            CheckContainerRestarts(startResults);

            return new CodexDeployment(gethResults, startResults.Select(r => r.Container).ToArray(), prometheusContainer, grafanaStartInfo, CreateMetadata());
        }

        private TestLifecycle CreateTestLifecycle()
        {
            var kubeConfig = GetKubeConfig(config.KubeConfigFile);

            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: kubeConfig,
                logPath: "null",
                logDebug: false,
                dataFilesPath: "notUsed",
                codexLogLevel: config.CodexLogLevel,
                k8sNamespacePrefix: config.KubeNamespace
            );

            var lifecycle = new TestLifecycle(new NullLog(), lifecycleConfig, timeset, string.Empty);
            DefaultContainerRecipe.TestsType = config.TestsTypePodLabel;
            DefaultContainerRecipe.ApplicationIds = lifecycle.GetApplicationIds();
            return lifecycle;
        }

        private (RunningContainer?, GrafanaStartInfo?) StartMetricsService(TestLifecycle lifecycle, CodexSetup setup, IEnumerable<RunningContainer> codexContainers)
        {
            if (setup.MetricsMode == DistTestCore.Metrics.MetricsMode.None) return (null, null);

            Log("Starting metrics service...");
            var runningContainers = new[] { new RunningContainers(null!, null!, codexContainers.ToArray()) };
            var prometheusContainer = lifecycle.PrometheusStarter.CollectMetricsFor(runningContainers).Containers.Single();

            if (setup.MetricsMode == DistTestCore.Metrics.MetricsMode.Record) return (prometheusContainer, null);

            Log("Starting dashboard service...");
            var grafanaStartInfo = lifecycle.GrafanaStarter.StartDashboard(prometheusContainer, setup);
            return (prometheusContainer, grafanaStartInfo);
        }

        private string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }

        private void CheckPeerConnectivity(List<CodexNodeStartResult> codexContainers)
        {
            if (!config.CheckPeerConnection) return;

            Log("Starting peer-connectivity check for deployed nodes...");
            peerConnectivityChecker.CheckConnectivity(codexContainers);
            Log("Check passed.");
        }

        private void CheckContainerRestarts(List<CodexNodeStartResult> startResults)
        {
            var crashes = new List<RunningContainer>();
            foreach (var startResult in startResults)
            {
                var watcher = startResult.Workflow.CreateCrashWatcher(startResult.Container);
                if (watcher.HasContainerCrashed()) crashes.Add(startResult.Container);
            }

            if (!crashes.Any())
            {
                Log("Container restart check passed.");
            }
            else
            {
                Log($"Deployment failed. The following containers have crashed: {string.Join(",", crashes.Select(c => c.Name))}");
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
