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

            log.Log("MarketplaceAddress: " + marketplaceAddress);
            return marketplaceAddress;
        }

        public (string, string) ExtractMarketplaceAbiAndByteCode()
        {
            log.Debug();
            var (abi, bytecode) = Retry(FetchMarketplaceAbiAndByteCode);
            if (string.IsNullOrEmpty(abi)) throw new InvalidOperationException("Unable to fetch marketplace artifacts from codex-contracts node. Test infra failure.");

            log.Debug("Got Marketplace ABI: " + abi);
            return (abi, bytecode);
        }

        private string FetchMarketplaceAddress()
        {
            var json = workflow.ExecuteCommand(container, "cat", CodexContractsContainerRecipe.DeployedAddressesFilename);
            json = json.Replace("#", "_");
            var addresses = JsonConvert.DeserializeObject<DeployedAddressesJson>(json);
            return addresses!.Marketplace_Marketplace;
        }

        private (string, string) FetchMarketplaceAbiAndByteCode()
        {
            var json = workflow.ExecuteCommand(container, "cat", CodexContractsContainerRecipe.MarketplaceArtifactFilename);

            var artifact = JObject.Parse(json);
            var abi = artifact["abi"];
            var byteCode = artifact["bytecode"];
            var abiResult = abi!.ToString(Formatting.None);
            var byteCodeResult = byteCode!.ToString(Formatting.None).ToLowerInvariant().Replace("\"", "");
          
            return (abiResult, byteCodeResult);
        }

        private static T Retry<T>(Func<T> fetch)
        {
            return Time.Retry(fetch, nameof(ContractsContainerInfoExtractor));
        }
    }

    public class DeployedAddressesJson
    {
        public string Token_TestToken { get; set; } = string.Empty;
        public string Verifier_Groth16Verifier { get; set; } = string.Empty;
        public string Marketplace_Marketplace { get; set; } = string.Empty;
    }
}
