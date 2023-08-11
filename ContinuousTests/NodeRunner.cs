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
        private readonly CodexAccess[] nodes;
        private readonly Configuration config;
        private readonly ITimeSet timeSet;
        private readonly BaseLog log;
        private readonly string customNamespace;
        private readonly int ethereumAccountIndex;

        public NodeRunner(CodexAccess[] nodes, Configuration config, ITimeSet timeSet, BaseLog log, string customNamespace, int ethereumAccountIndex)
        {
            this.nodes = nodes;
            this.config = config;
            this.timeSet = timeSet;
            this.log = log;
            this.customNamespace = customNamespace;
            this.ethereumAccountIndex = ethereumAccountIndex;
        }

        public void RunNode(Action<CodexAccess, MarketplaceAccess, TestLifecycle> operation)
        {
            RunNode(nodes.ToList().PickOneRandom(), operation, 0.TestTokens());
        }

        public void RunNode(CodexAccess bootstrapNode, Action<CodexAccess, MarketplaceAccess, TestLifecycle> operation)
        {
            RunNode(bootstrapNode, operation, 0.TestTokens());
        }

        public void RunNode(CodexAccess bootstrapNode, Action<CodexAccess, MarketplaceAccess, TestLifecycle> operation, TestToken mintTestTokens)
        {
            var lifecycle = CreateTestLifecycle();
            var flow = lifecycle.WorkflowCreator.CreateWorkflow();

            try
            {
                var debugInfo = bootstrapNode.GetDebugInfo();
                Assert.That(!string.IsNullOrEmpty(debugInfo.spr));

                var startupConfig = new StartupConfig();
                startupConfig.NameOverride = "TransientNode";
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
                var address = lifecycle.Configuration.GetAddress(container);
                var codexAccess = new CodexAccess(log, container, lifecycle.TimeSet, address);
                var marketAccess = new MarketplaceAccess(lifecycle, marketplaceNetwork, account, codexAccess);

                try
                {
                    operation(codexAccess, marketAccess, lifecycle);
                }
                catch
                {
                    lifecycle.DownloadLog(container);
                    throw;
                }
            }
            finally
            {
                flow.DeleteTestResources();
            }
        }

        private TestLifecycle CreateTestLifecycle()
        {
            return k8SFactory.CreateTestLifecycle(config.KubeConfigFile, config.LogPath, config.DataPath, customNamespace, timeSet, log);
        }
    }
}
