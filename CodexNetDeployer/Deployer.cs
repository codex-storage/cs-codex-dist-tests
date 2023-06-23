using DistTestCore;
using DistTestCore.Codex;
using KubernetesWorkflow;

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
            var (workflowCreator, lifecycle) = CreateFacilities();

            Log("Preparing configuration...");
            // We trick the Geth companion node into unlocking all of its accounts, by saying we want to start 999 codex nodes.
            var setup = new CodexSetup(999, config.CodexLogLevel);
            setup.WithStorageQuota(config.StorageQuota!.Value.MB()).EnableMarketplace(0.TestTokens());

            Log("Creating Geth instance and deploying contracts...");
            var gethStarter = new GethStarter(lifecycle, workflowCreator);
            var gethResults = gethStarter.BringOnlineMarketplaceFor(setup);

            Log("Geth started. Codex contracts deployed.");
            Log("Warning: It can take up to 45 minutes for the Geth node to finish unlocking all if its 1000 preconfigured accounts.");

            Log("Starting Codex nodes...");

            // Each node must have its own IP, so it needs it own pod. Start them 1 at a time.
            var codexStarter = new CodexNodeStarter(config, workflowCreator, lifecycle, log, timeset, gethResults, config.NumberOfValidators!.Value);
            var codexContainers = new List<RunningContainer>();
            for (var i = 0; i < config.NumberOfCodexNodes; i++)
            {
                var container = codexStarter.Start(i);
                if (container != null) codexContainers.Add(container);
            }

            return new CodexDeployment(gethResults, codexContainers.ToArray());
        }

        private (WorkflowCreator, TestLifecycle) CreateFacilities()
        {
            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: null, //config.KubeConfigFile,
                logPath: "null",
                logDebug: false,
                dataFilesPath: "notUsed",
                codexLogLevel: config.CodexLogLevel,
                runnerLocation: config.RunnerLocation
            );

            var kubeConfig = new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: config.KubeNamespace,
                kubeConfigFile: null, // config.KubeConfigFile,
                operationTimeout: timeset.K8sOperationTimeout(),
                retryDelay: timeset.WaitForK8sServiceDelay());

            var workflowCreator = new WorkflowCreator(log, kubeConfig);
            var lifecycle = new TestLifecycle(log, lifecycleConfig, timeset, workflowCreator);

            return (workflowCreator, lifecycle);
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
