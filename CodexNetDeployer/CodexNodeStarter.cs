using DistTestCore;
using DistTestCore.Codex;
using DistTestCore.Marketplace;
using KubernetesWorkflow;
using Logging;

namespace CodexNetDeployer
{
    public class CodexNodeStarter
    {
        private readonly Configuration config;
        private readonly WorkflowCreator workflowCreator;
        private readonly TestLifecycle lifecycle;
        private readonly BaseLog log;
        private readonly ITimeSet timeSet;
        private readonly GethStartResult gethResult;
        private string bootstrapSpr = "";
        private int validatorsLeft;

        public CodexNodeStarter(Configuration config, WorkflowCreator workflowCreator, TestLifecycle lifecycle, BaseLog log, ITimeSet timeSet, GethStartResult gethResult, int numberOfValidators)
        {
            this.config = config;
            this.workflowCreator = workflowCreator;
            this.lifecycle = lifecycle;
            this.log = log;
            this.timeSet = timeSet;
            this.gethResult = gethResult;
            this.validatorsLeft = numberOfValidators;
        }

        public RunningContainer? Start(int i)
        {
            Console.Write($" - {i} = ");
            var workflow = workflowCreator.CreateWorkflow();
            var workflowStartup = new StartupConfig();
            workflowStartup.Add(gethResult);
            workflowStartup.Add(CreateCodexStartupConfig(bootstrapSpr, i, validatorsLeft));

            var containers = workflow.Start(1, Location.Unspecified, new CodexContainerRecipe(), workflowStartup);

            var container = containers.Containers.First();
            var address = lifecycle.Configuration.GetAddress(container);
            var codexNode = new CodexNode(log, timeSet, address);
            var debugInfo = codexNode.GetDebugInfo();

            if (!string.IsNullOrWhiteSpace(debugInfo.spr))
            {
                var pod = container.Pod.PodInfo;
                Console.Write($"Online ({pod.Name} at {pod.Ip} on '{pod.K8SNodeName}')" + Environment.NewLine);

                if (string.IsNullOrEmpty(bootstrapSpr)) bootstrapSpr = debugInfo.spr;
                validatorsLeft--;
                return container;
            }
            else
            {
                Console.Write("Unknown failure." + Environment.NewLine);
                return null;
            }
        }

        private CodexStartupConfig CreateCodexStartupConfig(string bootstrapSpr, int i, int validatorsLeft)
        {
            var codexStart = new CodexStartupConfig(config.CodexLogLevel);

            if (!string.IsNullOrEmpty(bootstrapSpr)) codexStart.BootstrapSpr = bootstrapSpr;
            codexStart.StorageQuota = config.StorageQuota!.Value.MB();
            var marketplaceConfig = new MarketplaceInitialConfig(100000.Eth(), 0.TestTokens(), validatorsLeft > 0);
            marketplaceConfig.AccountIndexOverride = i;
            codexStart.MarketplaceConfig = marketplaceConfig;

            return codexStart;
        }
    }
}
