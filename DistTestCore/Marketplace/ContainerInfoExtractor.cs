using KubernetesWorkflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utils;

namespace DistTestCore.Marketplace
{
    public class ContainerInfoExtractor
    {
        private readonly StartupWorkflow workflow;
        private readonly RunningContainer container;

        public ContainerInfoExtractor(StartupWorkflow workflow, RunningContainer container)
        {
            this.workflow = workflow;
            this.container = container;
        }

        public string ExtractAccount()
        {
            var account = Retry(FetchAccount);
            if (string.IsNullOrEmpty(account)) throw new InvalidOperationException("Unable to fetch account for geth node. Test infra failure.");

            return account;
        }

        public string ExtractPubKey()
        {
            var pubKey = Retry(FetchPubKey);
            if (string.IsNullOrEmpty(pubKey)) throw new InvalidOperationException("Unable to fetch enode from geth node. Test infra failure.");

            return pubKey;
        }

        public string ExtractPrivateKey()
        {
            var privKey = Retry(FetchPrivateKey);
            if (string.IsNullOrEmpty(privKey)) throw new InvalidOperationException("Unable to fetch private key from geth node. Test infra failure.");

            return privKey;
        }

        public string ExtractMarketplaceAddress()
        {
            var marketplaceAddress = Retry(FetchMarketplaceAddress);
            if (string.IsNullOrEmpty(marketplaceAddress)) throw new InvalidOperationException("Unable to fetch marketplace account from codex-contracts node. Test infra failure.");

            return marketplaceAddress;
        }

        public string ExtractMarketplaceAbi()
        {
            var marketplaceAbi = Retry(FetchMarketplaceAbi);
            if (string.IsNullOrEmpty(marketplaceAbi)) throw new InvalidOperationException("Unable to fetch marketplace artifacts from codex-contracts node. Test infra failure.");

            return marketplaceAbi;
        }

        private string Retry(Func<string> fetch)
        {
            var result = string.Empty;
            Time.WaitUntil(() =>
            {
                result = Catch(fetch);
                return !string.IsNullOrEmpty(result);
            }, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(3));

            return result;
        }

        private string Catch(Func<string> fetch)
        {
            try
            {
                return fetch();
            }
            catch
            {
                return string.Empty;
            }
        }

        private string FetchAccount()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.AccountFilename);
        }

        private string FetchPrivateKey()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.PrivateKeyFilename);
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
            return abi!.ToString(Formatting.None);
        }

        private string FetchPubKey()
        {
            var enodeFinder = new PubKeyFinder();
            workflow.DownloadContainerLog(container, enodeFinder);
            return enodeFinder.GetPubKey();
        }
    }

    public class PubKeyFinder : LogHandler, ILogHandler
    {
        private const string openTag = "self=enode://";
        private const string openTagQuote = "self=\"enode://";
        private string pubKey = string.Empty;

        public string GetPubKey()
        {
            return pubKey;
        }

        protected override void ProcessLine(string line)
        {
            if (line.Contains(openTag))
            {
                ExtractPubKey(openTag, line);
            }
            else if (line.Contains(openTagQuote))
            {
                ExtractPubKey(openTagQuote, line);
            }
        }

        private void ExtractPubKey(string tag, string line)
        {
            var openIndex = line.IndexOf(tag) + tag.Length;
            var closeIndex = line.IndexOf("@");

            pubKey = line.Substring(
                    startIndex: openIndex,
                    length: closeIndex - openIndex);
        }
    }

    public class MarketplaceJson
    {
        public string address { get; set; } = string.Empty;
    }
}
