using DistTestCore.Codex;
using DistTestCore.Marketplace;
using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;
using Logging;

namespace ContinuousTests
{
    public class NodeRunner
    {
        private readonly CodexNode bootstrapNode;
        private readonly Configuration config;
        private readonly ITimeSet timeSet;
        private readonly BaseLog log;
        private readonly string customNamespace;
        private readonly int ethereumAccountIndex;

        public NodeRunner(CodexNode bootstrapNode, Configuration config, ITimeSet timeSet, BaseLog log, string customNamespace, int ethereumAccountIndex)
        {
            this.bootstrapNode = bootstrapNode;
            this.config = config;
            this.timeSet = timeSet;
            this.log = log;
            this.customNamespace = customNamespace;
            this.ethereumAccountIndex = ethereumAccountIndex;
        }

        public void RunNode(Action<CodexAccess, MarketplaceAccess> operation)
        {
            RunNode(operation, 0.TestTokens());
        }

        public void RunNode(Action<CodexAccess, MarketplaceAccess> operation, TestToken mintTestTokens)
        {
            var (workflowCreator, lifecycle) = CreateFacilities();
            var flow = workflowCreator.CreateWorkflow();

            try
            {
                var debugInfo = bootstrapNode.GetDebugInfo();
                Assert.That(!string.IsNullOrEmpty(debugInfo.spr));

                var startupConfig = new StartupConfig();
                var codexStartConfig = new CodexStartupConfig(CodexLogLevel.Trace);
                codexStartConfig.MarketplaceConfig = new MarketplaceInitialConfig(0.Eth(), 0.TestTokens(), false);
                codexStartConfig.MarketplaceConfig.AccountIndexOverride = ethereumAccountIndex;
                codexStartConfig.BootstrapSpr = debugInfo.spr;
                startupConfig.Add(codexStartConfig);
                startupConfig.Add(config.CodexDeployment.GethStartResult);
                var rc = flow.Start(1, Location.Unspecified, new CodexContainerRecipe(), startupConfig);

                var account = config.CodexDeployment.GethStartResult.CompanionNode.Accounts[ethereumAccountIndex];

                var marketplaceNetwork = config.CodexDeployment.GethStartResult.MarketplaceNetwork;
                if (mintTestTokens.Amount > 0)
                {
                    var tokenAddress = marketplaceNetwork.Marketplace.TokenAddress;
                    var interaction = marketplaceNetwork.Bootstrap.StartInteraction(lifecycle);
                    interaction.MintTestTokens(new[] { account.Account }, mintTestTokens.Amount, tokenAddress);
                }

                var container = rc.Containers[0];
                var codexAccess = new CodexAccess(lifecycle, container);
                var marketAccess = new MarketplaceAccess(lifecycle, marketplaceNetwork, account, codexAccess);

                operation(codexAccess, marketAccess);
            }
            finally
            {
                flow.DeleteTestResources();
            }
        }

        private (WorkflowCreator, TestLifecycle) CreateFacilities()
        {
            var kubeConfig = GetKubeConfig(config.KubeConfigFile);
            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: kubeConfig,
                logPath: "null",
                logDebug: false,
                dataFilesPath: config.LogPath,
                codexLogLevel: CodexLogLevel.Debug,
                runnerLocation: TestRunnerLocation.ExternalToCluster
            );

            var kubeFlowConfig = new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: customNamespace,
                kubeConfigFile: kubeConfig,
                operationTimeout: timeSet.K8sOperationTimeout(),
            retryDelay: timeSet.WaitForK8sServiceDelay());

            var workflowCreator = new WorkflowCreator(log, kubeFlowConfig, testNamespacePostfix: string.Empty);
            var lifecycle = new TestLifecycle(new NullLog(), lifecycleConfig, timeSet, workflowCreator);

            return (workflowCreator, lifecycle);
        }

        private static string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }
    }
}
