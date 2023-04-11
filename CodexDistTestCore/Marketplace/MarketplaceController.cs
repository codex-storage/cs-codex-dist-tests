using NUnit.Framework;
using System.Text;

namespace CodexDistTestCore.Marketplace
{
    public class MarketplaceController
    {
        private readonly TestLog log;
        private readonly K8sManager k8sManager;
        private readonly NumberSource companionGroupNumberSource = new NumberSource(0);
        private List<GethCompanionGroup> companionGroups = new List<GethCompanionGroup>();
        private GethBootstrapInfo? bootstrapInfo;

        public MarketplaceController(TestLog log, K8sManager k8sManager)
        {
            this.log = log;
            this.k8sManager = k8sManager;
        }

        public GethCompanionGroup BringOnlineMarketplace(OfflineCodexNodes offline)
        {
            if (bootstrapInfo == null)
            {
                BringOnlineBootstrapNode();
            }

            log.Log($"Initializing companions for {offline.NumberOfNodes} Codex nodes.");

            var group = new GethCompanionGroup(companionGroupNumberSource.GetNextNumber(), CreateCompanionContainers(offline));
            group.Pod = k8sManager.BringOnlineGethCompanionGroup(bootstrapInfo!, group);
            companionGroups.Add(group);

            log.Log("Initialized companion nodes.");
            return group;
        }

        private void BringOnlineBootstrapNode()
        {
            log.Log("Starting Geth bootstrap node...");
            var spec = k8sManager.CreateGethBootstrapNodeSpec();
            var pod = k8sManager.BringOnlineGethBootstrapNode(spec);
            var (account, genesisJson) = ExtractAccountAndGenesisJson();
            bootstrapInfo = new GethBootstrapInfo(spec, pod, account, genesisJson);
            log.Log($"Geth boothstrap node started.");
        }

        private GethCompanionNodeContainer[] CreateCompanionContainers(OfflineCodexNodes offline)
        {
            var numberSource = new NumberSource(8080);
            var result = new List<GethCompanionNodeContainer>();
            for (var i = 0; i < offline.NumberOfNodes; i++) result.Add(CreateGethNodeContainer(numberSource, i));
            return result.ToArray();
        }

        private GethCompanionNodeContainer CreateGethNodeContainer(NumberSource numberSource, int n)
        {
            return new GethCompanionNodeContainer(
                name: $"geth-node{n}",
                apiPort: numberSource.GetNextNumber(),
                rpcPort: numberSource.GetNextNumber(),
                containerPortName: $"geth-{n}"
            );
        }

        public void AddToBalance(string account, int amount)
        {
            if (amount < 1 || string.IsNullOrEmpty(account)) Assert.Fail("Invalid arguments for AddToBalance");

            // call the bootstrap node and convince it to give 'account' 'amount' tokens somehow.
            throw new NotImplementedException();
        }

        private (string, string) ExtractAccountAndGenesisJson()
        {
            var (account, genesisJson) = FetchAccountAndGenesisJson();
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(genesisJson))
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                (account, genesisJson) = FetchAccountAndGenesisJson();
            }

            Assert.That(account, Is.Not.Empty, "Unable to fetch account for geth bootstrap node. Test infra failure.");
            Assert.That(genesisJson, Is.Not.Empty, "Unable to fetch genesis-json for geth bootstrap node. Test infra failure.");

            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(genesisJson));

            log.Log($"Initialized geth bootstrap node with account '{account}'");
            return (account, encoded);
        }

        private (string, string) FetchAccountAndGenesisJson()
        {
            var bootstrapAccount = ExecuteCommand("cat", GethDockerImage.AccountFilename);
            var bootstrapGenesisJson = ExecuteCommand("cat", GethDockerImage.GenesisFilename);
            return (bootstrapAccount, bootstrapGenesisJson);
        }

        private string ExecuteCommand(string command, params string[] arguments)
        {
            return k8sManager.ExecuteCommand(bootstrapInfo!.Pod, K8sGethBoostrapSpecs.ContainerName, command, arguments);
        }
    }

    public class GethBootstrapInfo
    {
        public GethBootstrapInfo(K8sGethBoostrapSpecs spec, PodInfo pod, string account, string genesisJsonBase64)
        {
            Spec = spec;
            Pod = pod;
            Account = account;
            GenesisJsonBase64 = genesisJsonBase64;
        }

        public K8sGethBoostrapSpecs Spec { get; }
        public PodInfo Pod { get; }
        public string Account { get; }
        public string GenesisJsonBase64 { get; }
    }
}
