using KubernetesWorkflow;
using Utils;

namespace DistTestCore.Marketplace
{
    public class CodexContractsStarter : BaseStarter
    {
        private const string readyString = "Done! Sleeping indefinitely...";

        public CodexContractsStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
        }

        public MarketplaceInfo Start(GethBootstrapNodeInfo bootstrapNode)
        {
            LogStart("Deploying Codex contracts...");

            var workflow = workflowCreator.CreateWorkflow();
            var startupConfig = CreateStartupConfig(bootstrapNode.RunningContainers.Containers[0]);

            var containers = workflow.Start(1, Location.Unspecified, new CodexContractsContainerRecipe(), startupConfig);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Codex contracts container to be created. Test infra failure.");
            var container = containers.Containers[0];

            WaitUntil(() =>
            {
                var logHandler = new ContractsReadyLogHandler(readyString);
                workflow.DownloadContainerLog(container, logHandler);
                return logHandler.Found;
            });

            var extractor = new ContainerInfoExtractor(workflow, container);
            var marketplaceAddress = extractor.ExtractMarketplaceAddress();
            var abi = extractor.ExtractMarketplaceAbi();

            var interaction = bootstrapNode.StartInteraction(lifecycle.Log);
            var tokenAddress = interaction.GetTokenAddress(marketplaceAddress);

            LogEnd("Contracts deployed.");

            return new MarketplaceInfo(marketplaceAddress, abi, tokenAddress);
        }

        private void WaitUntil(Func<bool> predicate)
        {
            Time.WaitUntil(predicate, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(1));
        }

        private StartupConfig CreateStartupConfig(RunningContainer bootstrapContainer)
        {
            var startupConfig = new StartupConfig();
            var contractsConfig = new CodexContractsContainerConfig(bootstrapContainer.Pod.Ip, bootstrapContainer.Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag));
            startupConfig.Add(contractsConfig);
            return startupConfig;
        }
    }

    public class MarketplaceInfo
    {
        public MarketplaceInfo(string address, string abi, string tokenAddress)
        {
            Address = address;
            Abi = abi;
            TokenAddress = tokenAddress;
        }

        public string Address { get; }
        public string Abi { get; }
        public string TokenAddress { get; }
    }

    public class ContractsReadyLogHandler : LogHandler
    {
        private readonly string targetString;

        public ContractsReadyLogHandler(string targetString)
        {
            this.targetString = targetString;
        }

        public bool Found { get; private set; }

        protected override void ProcessLine(string line)
        {
            if (line.Contains(targetString)) Found = true;
        }
    }
}
