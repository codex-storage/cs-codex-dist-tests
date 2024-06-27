using CodexContractsPlugin.Marketplace;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utils;

namespace CodexContractsPlugin
{
    public class ContractsContainerInfoExtractor
    {
        private readonly ILog log;
        private readonly IStartupWorkflow workflow;
        private readonly RunningContainer container;

        public ContractsContainerInfoExtractor(ILog log, IStartupWorkflow workflow, RunningContainer container)
        {
            this.log = log;
            this.workflow = workflow;
            this.container = container;
        }

        public string ExtractMarketplaceAddress()
        {
            log.Debug();
            var marketplaceAddress = Retry(FetchMarketplaceAddress);
            if (string.IsNullOrEmpty(marketplaceAddress)) throw new InvalidOperationException("Unable to fetch marketplace account from codex-contracts node. Test infra failure.");

            log.Debug("Got MarketplaceAddress: " + marketplaceAddress);
            return marketplaceAddress;
        }

        public string ExtractMarketplaceAbi()
        {
            log.Debug();
            var marketplaceAbi = Retry(FetchMarketplaceAbi);
            if (string.IsNullOrEmpty(marketplaceAbi)) throw new InvalidOperationException("Unable to fetch marketplace artifacts from codex-contracts node. Test infra failure.");

            log.Debug("Got Marketplace ABI: " + marketplaceAbi);
            return marketplaceAbi;
        }

        private string FetchMarketplaceAddress()
        {
            var json = workflow.ExecuteCommand(container, "cat", CodexContractsContainerRecipe.MarketplaceAddressFilename);
            var marketplace = JsonConvert.DeserializeObject<MarketplaceJson>(json);
            return marketplace!.address;
        }

        private string FetchMarketplaceAbi()
        {
            var json = workflow.ExecuteCommand(container, "cat", CodexContractsContainerRecipe.MarketplaceArtifactFilename);

            var artifact = JObject.Parse(json);
            var abi = artifact["abi"];
            var byteCode = artifact["bytecode"];
            var abiResult = abi!.ToString(Formatting.None);
            var byteCodeResult = byteCode!.ToString(Formatting.None);

            if (byteCodeResult
                .ToLowerInvariant()
                .Replace("\"", "") != MarketplaceDeploymentBase.BYTECODE.ToLowerInvariant())
            {
                throw new Exception("BYTECODE in CodexContractsPlugin does not match BYTECODE deployed by container. Update Marketplace.cs generated code?");
            }

            return abiResult;
        }

        private static string Retry(Func<string> fetch)
        {
            return Time.Retry(fetch, nameof(ContractsContainerInfoExtractor));
        }
    }

    public class MarketplaceJson
    {
        public string address { get; set; } = string.Empty;
    }
}
