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

        public GethInfo BringOnlineMarketplace(OfflineCodexNodes offline)
        {
            if (gethBootstrapNode != null) return gethBootstrapNode;

            log.Log("Starting Geth bootstrap node...");
            gethBootstrapNode = k8sManager.BringOnlineGethBootstrapNode();
            ExtractAccountAndGenesisJson();
            log.Log($"Geth boothstrap node started. Initializing companions for {offline.NumberOfNodes} Codex nodes.");




            return gethBootstrapNode;
        }


        private GethCompanionNodeContainer? CreateGethNodeContainer(OfflineCodexNodes offline, int n)
        {
            return new GethCompanionNodeContainer(
                name: $"geth-node{n}",
                servicePort: numberSource.GetNextServicePort(),
                servicePortName: numberSource.GetNextServicePortName(),
                apiPort: codexPortSource.GetNextNumber(),
                rpcPort: codexPortSource.GetNextNumber(),
                containerPortName: $"geth-{n}"
            );
        }

        public void AddToBalance(string account, int amount)
        {
            if (amount < 1 || string.IsNullOrEmpty(account)) Assert.Fail("Invalid arguments for AddToBalance");

            // call the bootstrap node and convince it to give 'account' 'amount' tokens somehow.
            throw new NotImplementedException();
        }

        private void ExtractAccountAndGenesisJson()
        {
            FetchAccountAndGenesisJson();
            if (string.IsNullOrEmpty(bootstrapAccount) || string.IsNullOrEmpty(bootstrapGenesisJson))
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                FetchAccountAndGenesisJson();
            }

            Assert.That(bootstrapAccount, Is.Not.Empty, "Unable to fetch account for geth bootstrap node. Test infra failure.");
            Assert.That(bootstrapGenesisJson, Is.Not.Empty, "Unable to fetch genesis-json for geth bootstrap node. Test infra failure.");

            gethBootstrapNode!.GenesisJsonBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(bootstrapGenesisJson));

            log.Log($"Initialized geth bootstrap node with account '{bootstrapAccount}'");
        }

        private void FetchAccountAndGenesisJson()
        {
            bootstrapAccount = ExecuteCommand("cat", GethDockerImage.AccountFilename);
            bootstrapGenesisJson = ExecuteCommand("cat", GethDockerImage.GenesisFilename);
        }

        private string ExecuteCommand(string command, params string[] arguments)
        {
            return k8sManager.ExecuteCommand(gethBootstrapNode!.BootstrapPod, K8sGethBoostrapSpecs.ContainerName, command, arguments);
        }
    }

    public class GethInfo
    {
        public GethInfo(K8sGethBoostrapSpecs spec, PodInfo bootstrapPod, PodInfo companionPod)
        {
            Spec = spec;
            BootstrapPod = bootstrapPod;
            CompanionPod = companionPod;
        }

        public K8sGethBoostrapSpecs Spec { get; }
        public PodInfo BootstrapPod { get; }
        public PodInfo CompanionPod { get; }
        public string GenesisJsonBase64 { get; set; } = string.Empty;
    }
}
