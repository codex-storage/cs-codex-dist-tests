using NUnit.Framework;
using System.Text;

namespace CodexDistTestCore.Marketplace
{
    public class MarketplaceController
    {
        private readonly TestLog log;
        private readonly K8sManager k8sManager;
        private GethInfo? gethBootstrapNode;
        private string bootstrapAccount = string.Empty;
        private string bootstrapGenesisJson = string.Empty;

        public MarketplaceController(TestLog log, K8sManager k8sManager)
        {
            this.log = log;
            this.k8sManager = k8sManager;
        }

        public GethInfo BringOnlineMarketplace()
        {
            if (gethBootstrapNode != null) return gethBootstrapNode;

            log.Log("Starting Geth bootstrap node...");
            gethBootstrapNode = k8sManager.BringOnlineGethBootstrapNode();
            ExtractAccountAndGenesisJson();
            log.Log("Geth boothstrap node started.");

            return gethBootstrapNode;
        }

        private void ExtractAccountAndGenesisJson()
        {
            bootstrapAccount = ExecuteCommand("cat", "account_string.txt");
            bootstrapGenesisJson = ExecuteCommand("cat", "genesis.json");

            Assert.That(bootstrapAccount, Is.Not.Empty, "Unable to fetch account for bootstrap geth node. Test infra failure.");
            Assert.That(bootstrapGenesisJson, Is.Not.Empty, "Unable to fetch genesis-json for bootstrap geth node. Test infra failure.");

            gethBootstrapNode!.GenesisJsonBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(bootstrapGenesisJson));
        }

        private string ExecuteCommand(string command, params string[] arguments)
        {
            return k8sManager.ExecuteCommand(gethBootstrapNode!.Pod, K8sGethBoostrapSpecs.ContainerName, command, arguments);
        }
    }

    public class GethInfo
    {
        public GethInfo(K8sGethBoostrapSpecs spec, PodInfo pod)
        {
            Spec = spec;
            Pod = pod;
        }

        public K8sGethBoostrapSpecs Spec { get; }
        public PodInfo Pod { get; }
        public string GenesisJsonBase64 { get; set; } = string.Empty;
    }
}
