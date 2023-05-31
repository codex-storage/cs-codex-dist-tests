using KubernetesWorkflow;
using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utils;

namespace DistTestCore.Marketplace
{
    public class ContainerInfoExtractor
    {
        private readonly BaseLog log;
        private readonly StartupWorkflow workflow;
        private readonly RunningContainer container;

        public ContainerInfoExtractor(BaseLog log, StartupWorkflow workflow, RunningContainer container)
        {
            this.log = log;
            this.workflow = workflow;
            this.container = container;
        }

        public AllGethAccounts ExtractAccounts()
        {
            log.Debug();
            var accountsCsv = Retry(() => FetchAccountsCsv());
            if (string.IsNullOrEmpty(accountsCsv)) throw new InvalidOperationException("Unable to fetch accounts.csv for geth node. Test infra failure.");

            var lines = accountsCsv.Split('\n');
            return new AllGethAccounts(lines.Select(ParseLineToAccount).ToArray());
        }

        public string ExtractPubKey()
        {
            log.Debug();
            var pubKey = Retry(FetchPubKey);
            if (string.IsNullOrEmpty(pubKey)) throw new InvalidOperationException("Unable to fetch enode from geth node. Test infra failure.");

            return pubKey;
        }

        public string ExtractMarketplaceAddress()
        {
            log.Debug();
            var marketplaceAddress = Retry(FetchMarketplaceAddress);
            if (string.IsNullOrEmpty(marketplaceAddress)) throw new InvalidOperationException("Unable to fetch marketplace account from codex-contracts node. Test infra failure.");

            return marketplaceAddress;
        }

        public string ExtractMarketplaceAbi()
        {
            log.Debug();
            var marketplaceAbi = Retry(FetchMarketplaceAbi);
            if (string.IsNullOrEmpty(marketplaceAbi)) throw new InvalidOperationException("Unable to fetch marketplace artifacts from codex-contracts node. Test infra failure.");

            return marketplaceAbi;
        }

        private string FetchAccountsCsv()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.AccountsFilename);
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

        private GethAccount ParseLineToAccount(string l)
        {
            var tokens = l.Replace("\r", "").Split(',');
            if (tokens.Length != 2) throw new InvalidOperationException();
            var account = tokens[0];
            var privateKey = tokens[1];
            return new GethAccount(account, privateKey);
        }

        private static string Retry(Func<string> fetch)
        {
            return Time.Retry(fetch, nameof(ContainerInfoExtractor));
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
