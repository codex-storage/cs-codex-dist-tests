using NUnit.Framework;

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

            Assert.That(bootstrapAccount, Is.Not.Empty, "Unable to fetch account for bootstrap geth node. Test infra failure.");
            Assert.That(bootstrapGenesisJson, Is.Not.Empty, "Unable to fetch genesis-json for bootstrap geth node. Test infra failure.");
        }

        private string ExecuteCommand(string command, params string[] arguments)
        {
            return k8sManager.ExecuteCommand(gethBootstrapNode!, K8sGethBoostrapSpecs.ContainerName, command, arguments);
        }
    }
}
