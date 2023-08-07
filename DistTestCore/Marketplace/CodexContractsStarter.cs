using KubernetesWorkflow;
using Utils;

namespace DistTestCore.Marketplace
{
    public class CodexContractsStarter : BaseStarter
    {

        public CodexContractsStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
            : base(lifecycle, workflowCreator)
        {
        }

        public MarketplaceInfo Start(GethBootstrapNodeInfo bootstrapNode)
        {
            LogStart("Deploying Codex Marketplace...");

            var workflow = workflowCreator.CreateWorkflow();
            var startupConfig = CreateStartupConfig(bootstrapNode.RunningContainers.Containers[0]);

            var containers = workflow.Start(1, Location.Unspecified, new CodexContractsContainerRecipe(), startupConfig);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Codex contracts container to be created. Test infra failure.");
            var container = containers.Containers[0];

            WaitUntil(() =>
            {
                var logHandler = new ContractsReadyLogHandler(Debug);
                workflow.DownloadContainerLog(container, logHandler);
                return logHandler.Found;
            });
            Log("Contracts deployed. Extracting addresses...");

            var extractor = new ContainerInfoExtractor(lifecycle.Log, workflow, container);
            var marketplaceAddress = extractor.ExtractMarketplaceAddress();
            var abi = extractor.ExtractMarketplaceAbi();

            var interaction = bootstrapNode.StartInteraction(lifecycle);
            var tokenAddress = interaction.GetTokenAddress(marketplaceAddress);

            LogEnd("Extract completed. Marketplace deployed.");

            return new MarketplaceInfo(marketplaceAddress, abi, tokenAddress);
        }

        private void WaitUntil(Func<bool> predicate)
        {
            Time.WaitUntil(predicate, TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(2));
        }

        private StartupConfig CreateStartupConfig(RunningContainer bootstrapContainer)
        {
            var startupConfig = new StartupConfig();
            var contractsConfig = new CodexContractsContainerConfig(bootstrapContainer.Pod.PodInfo.Ip, bootstrapContainer.Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag));
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
        // Log should contain 'Compiled 15 Solidity files successfully' at some point.
        private const string RequiredCompiledString = "Solidity files successfully";
        // When script is done, it prints the ready-string.
        private const string ReadyString = "Done! Sleeping indefinitely...";
        private readonly Action<string> debug;

        public ContractsReadyLogHandler(Action<string> debug)
        {
            this.debug = debug;
            debug($"Looking for '{RequiredCompiledString}' and '{ReadyString}' in container logs...");
        }

        public bool SeenCompileString { get; private set; }
        public bool Found { get; private set; }

        protected override void ProcessLine(string line)
        {
            debug(line);
            if (line.Contains(RequiredCompiledString)) SeenCompileString = true;
            if (line.Contains(ReadyString))
            {
                if (!SeenCompileString) throw new Exception("CodexContracts deployment failed. " +
                    "Solidity files not compiled before process exited.");

                Found = true;
            }
        }
    }
}
