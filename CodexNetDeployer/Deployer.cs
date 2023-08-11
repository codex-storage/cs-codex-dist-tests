using DistTestCore;
using DistTestCore.Codex;
using KubernetesWorkflow;
using Logging;

namespace CodexNetDeployer
{
    public class Deployer
    {
        private readonly Configuration config;
        private readonly NullLog log;
        private readonly DefaultTimeSet timeset;

        public Deployer(Configuration config)
        {
            this.config = config;
            log = new NullLog();
            timeset = new DefaultTimeSet();
        }

        public CodexDeployment Deploy()
        {
            Log("Initializing...");
            var lifecycle = CreateTestLifecycle();

            Log("Preparing configuration...");
            // We trick the Geth companion node into unlocking all of its accounts, by saying we want to start 999 codex nodes.
            var setup = new CodexSetup(999, config.CodexLogLevel);
            setup.WithStorageQuota(config.StorageQuota!.Value.MB()).EnableMarketplace(0.TestTokens());
            setup.MetricsEnabled = config.RecordMetrics;

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
            var codexContainers = new List<RunningContainer>();
            for (var i = 0; i < config.NumberOfCodexNodes; i++)
            {
                var container = codexStarter.Start(i);
                if (container != null) codexContainers.Add(container);
            }

            var prometheusContainer = StartMetricsService(lifecycle, setup, codexContainers);

            return new CodexDeployment(gethResults, codexContainers.ToArray(), prometheusContainer, CreateMetadata());
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

            return new TestLifecycle(log, lifecycleConfig, timeset, config.TestsTypePodLabel, string.Empty);
        }

        private RunningContainer? StartMetricsService(TestLifecycle lifecycle, CodexSetup setup, List<RunningContainer> codexContainers)
        {
            if (!setup.MetricsEnabled) return null;

            Log("Starting metrics service...");
            var runningContainers = new[] { new RunningContainers(null!, null!, codexContainers.ToArray()) };
            return lifecycle.PrometheusStarter.CollectMetricsFor(runningContainers).Containers.Single();
        }

        private string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
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
                maxDuration: config.MaxDuration);
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
