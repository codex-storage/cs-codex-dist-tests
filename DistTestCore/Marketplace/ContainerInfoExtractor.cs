using KubernetesWorkflow;
using Newtonsoft.Json;
using System.Text;

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

        public string ExtractGenesisJsonBase64()
        {
            var genesisJson = Retry(FetchGenesisJson);
            if (string.IsNullOrEmpty(genesisJson)) throw new InvalidOperationException("Unable to fetch genesis-json for geth node. Test infra failure.");

            return Convert.ToBase64String(Encoding.ASCII.GetBytes(genesisJson));
        }

        public string ExtractPubKey()
        {
            var pubKey = Retry(FetchPubKey);
            if (string.IsNullOrEmpty(pubKey)) throw new InvalidOperationException("Unable to fetch enode from geth node. Test infra failure.");

            return pubKey;
        }

        public string ExtractBootstrapPrivateKey()
        {
            var privKey = Retry(FetchBootstrapPrivateKey);
            if (string.IsNullOrEmpty(privKey)) throw new InvalidOperationException("Unable to fetch private key from geth node. Test infra failure.");

            return privKey;
        }

        public string ExtractMarketplaceAddress()
        {
            var marketplaceAddress = Retry(FetchMarketplaceAddress);
            if (string.IsNullOrEmpty(marketplaceAddress)) throw new InvalidOperationException("Unable to fetch marketplace account from codex-contracts node. Test infra failure.");

            return marketplaceAddress;
        }

        private string Retry(Func<string> fetch)
        {
            var result = Catch(fetch);
            if (string.IsNullOrEmpty(result))
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                result = fetch();
            }
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

        private string FetchGenesisJson()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.GenesisFilename);
        }

        private string FetchAccount()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.AccountFilename);
        }

        private string FetchBootstrapPrivateKey()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.BootstrapPrivateKeyFilename);
        }

        private string FetchMarketplaceAddress()
        {
            var json = workflow.ExecuteCommand(container, "cat", CodexContractsContainerRecipe.MarketplaceAddressFilename);
            var marketplace = JsonConvert.DeserializeObject<MarketplaceJson>(json);
            return marketplace!.address;
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
        private const string openTag = "self=\"enode://";
        private string pubKey = string.Empty;

        public string GetPubKey()
        {
            return pubKey;
        }

        protected override void ProcessLine(string line)
        {
            if (line.Contains(openTag))
            {
                ExtractPubKey(line);
            }
        }

        private void ExtractPubKey(string line)
        {
            var openIndex = line.IndexOf(openTag) + openTag.Length;
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
