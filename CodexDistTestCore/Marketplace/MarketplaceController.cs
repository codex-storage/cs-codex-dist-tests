namespace CodexDistTestCore.Marketplace
{
    public class MarketplaceController
    {
        private readonly TestLog log;
        private readonly K8sManager k8sManager;
        private PodInfo? gethBootstrapNode;
        private string bootstrapAccount = string.Empty;
        private string bootstrapGenesisJson = string.Empty;

        public MarketplaceController(TestLog log, K8sManager k8sManager)
        {
            this.log = log;
            this.k8sManager = k8sManager;
        }

        public void BringOnlineMarketplace()
        {
            if (gethBootstrapNode != null) return;

            log.Log("Starting Geth bootstrap node...");
            gethBootstrapNode = k8sManager.BringOnlineGethBootstrapNode();
            ExtractAccountAndGenesisJson();
            log.Log("Geth boothstrap node started.");
        }

        private void ExtractAccountAndGenesisJson()
        {
            bootstrapAccount = ExecuteCommand("cat", "account_string.txt");
            bootstrapGenesisJson = ExecuteCommand("cat", "genesis.json");
        }

        private string ExecuteCommand(string command, params string[] arguments)
        {
            return k8sManager.ExecuteCommand(gethBootstrapNode!, K8sGethBoostrapSpecs.ContainerName, command, arguments);
        }
    }
}
