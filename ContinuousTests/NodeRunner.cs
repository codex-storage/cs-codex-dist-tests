using DistTestCore.Codex;
using DistTestCore.Marketplace;
using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;
using Logging;
using Utils;

namespace ContinuousTests
{
    public class NodeRunner
    {
        private readonly K8sFactory k8SFactory = new K8sFactory();
        private readonly CodexNode[] nodes;
        private readonly Configuration config;
        private readonly ITimeSet timeSet;
        private readonly BaseLog log;
        private readonly string customNamespace;
        private readonly int ethereumAccountIndex;

        public NodeRunner(CodexNode[] nodes, Configuration config, ITimeSet timeSet, BaseLog log, string customNamespace, int ethereumAccountIndex)
        {
            this.nodes = nodes;
            this.config = config;
            this.timeSet = timeSet;
            this.log = log;
            this.customNamespace = customNamespace;
            this.ethereumAccountIndex = ethereumAccountIndex;
        }

        public void RunNode(Action<CodexAccess, MarketplaceAccess> operation)
        {
            RunNode(nodes.ToList().PickOneRandom(), operation, 0.TestTokens());
        }

        public void RunNode(CodexNode bootstrapNode, Action<CodexAccess, MarketplaceAccess> operation)
        {
            RunNode(bootstrapNode, operation, 0.TestTokens());
        }

        public void RunNode(CodexNode bootstrapNode, Action<CodexAccess, MarketplaceAccess> operation, TestToken mintTestTokens)
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
            return k8SFactory.CreateFacilities(config.KubeConfigFile, config.LogPath, config.DataPath, customNamespace, timeSet, log);
        }
    }
}
