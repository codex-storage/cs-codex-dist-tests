using DistTestCore;
using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;
using System.ComponentModel;

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

        public void Deploy()
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

            Log("Starting Codex nodes...");

            // Each node must have its own IP, so it needs it own pod. Start them 1 at a time.
            var bootstrapSpr = ""; // The first one will be used to bootstrap the others.
            int validatorsLeft = config.NumberOfValidators!.Value;
            for (var i = 0; i < config.NumberOfCodexNodes; i++)
            {
                Console.Write($" - {i} = ");
                var workflow = workflowCreator.CreateWorkflow();
                var workflowStartup = new StartupConfig();
                var codexStart = new CodexStartupConfig(config.CodexLogLevel);
                if (!string.IsNullOrEmpty(bootstrapSpr)) codexStart.BootstrapSpr = bootstrapSpr;
                codexStart.StorageQuota = config.StorageQuota.Value.MB();
                var marketplaceConfig = new MarketplaceInitialConfig(100000.Eth(), 0.TestTokens(), validatorsLeft > 0);
                marketplaceConfig.AccountIndexOverride = i;
                codexStart.MarketplaceConfig = marketplaceConfig;
                workflowStartup.Add(gethResults);

                var containers = workflow.Start(1, Location.Unspecified, new CodexContainerRecipe(), workflowStartup);

                var container = containers.Containers.First();
                var address = lifecycle.Configuration.GetAddress(container);
                var codexNode = new CodexNode(log, timeset, address);
                var debugInfo = codexNode.GetDebugInfo();

                if (!string.IsNullOrWhiteSpace(debugInfo.spr))
                {
                    var pod = container.Pod.PodInfo;
                    Console.Write($"Online ({pod.Name} at {pod.Ip} on '{pod.K8SNodeName}'" + Environment.NewLine);

                    if (string.IsNullOrEmpty(bootstrapSpr)) bootstrapSpr = debugInfo.spr;
                    validatorsLeft--;
                }
                else
                {
                    Console.Write("Unknown failure." + Environment.NewLine);
                }
            }
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
