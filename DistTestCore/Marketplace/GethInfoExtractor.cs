using KubernetesWorkflow;
using System.Text;

namespace DistTestCore.Marketplace
{
    public class GethInfoExtractor
    {
        private readonly StartupWorkflow workflow;
        private readonly RunningContainer container;

        public GethInfoExtractor(StartupWorkflow workflow, RunningContainer container)
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

            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(genesisJson));

            return encoded;
        }

        private string Retry(Func<string> fetch)
        {
            var result = fetch();
            if (string.IsNullOrEmpty(result))
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                result = fetch();
            }
            return result;
        }

        private string FetchGenesisJson()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.GenesisFilename);
        }

        private string FetchAccount()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.AccountFilename);
        }
    }
}
