using CodexDistTestCore.Config;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Web3.Accounts.Managed;
using NUnit.Framework;
using System.Numerics;
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
            var (account, genesisJson) = ExtractAccountAndGenesisJson(pod);
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
                authRpcPort: numberSource.GetNextNumber(),
                rpcPort: numberSource.GetNextNumber(),
                containerPortName: $"geth-{n}"
            );
        }

        private readonly K8sCluster k8sCluster = new K8sCluster();

        public void AddToBalance(string account, decimal amount)
        {
            if (amount < 1 || string.IsNullOrEmpty(account)) Assert.Fail("Invalid arguments for AddToBalance");

            // call the bootstrap node and convince it to give 'account' 'amount' tokens somehow.

            var web3 = CreateWeb3();

            //var blockNumber1 = Utils.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            //Thread.Sleep(TimeSpan.FromSeconds(5));
            //var blockNumber2 = Utils.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());

            //var bootstrapBalance = Utils.Wait(web3.Eth.GetBalance.SendRequestAsync(bootstrapInfo.Account));
            

            var bigint = new BigInteger(amount);
            var str = bigint.ToString("X");
            var value = new Nethereum.Hex.HexTypes.HexBigInteger(str);
            var aaa = Utils.Wait(web3.Eth.TransactionManager.SendTransactionAsync(bootstrapInfo!.Account, account, value));
            var receipt = Utils.Wait(web3.Eth.TransactionManager.TransactionReceiptService.PollForReceiptAsync(aaa));

            //var receipt = Utils.Wait(web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(account, amount));
            //var targetBalance = Utils.Wait(web3.Eth.GetBalance.SendRequestAsync(account));
        }

        public decimal GetBalance(string account)
        {
            var web3 = CreateWeb3();
            var bigInt = Utils.Wait(web3.Eth.GetBalance.SendRequestAsync(account));
            return (decimal)bigInt.Value;
        }

        private Web3 CreateWeb3()
        {
            var ip = k8sCluster.GetIp();
            var port = bootstrapInfo!.Spec.ServicePort;
            //var bootstrapaccount = new ManagedAccount(bootstrapInfo.Account, "qwerty!@#$%^");
            return new Web3($"http://{ip}:{port}");
        }

        private (string, string) ExtractAccountAndGenesisJson(PodInfo pod)
        {
            var (account, genesisJson) = FetchAccountAndGenesisJson(pod);
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(genesisJson))
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                (account, genesisJson) = FetchAccountAndGenesisJson(pod);
            }

            Assert.That(account, Is.Not.Empty, "Unable to fetch account for geth bootstrap node. Test infra failure.");
            Assert.That(genesisJson, Is.Not.Empty, "Unable to fetch genesis-json for geth bootstrap node. Test infra failure.");

            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(genesisJson));

            log.Log($"Initialized geth bootstrap node with account '{account}'");
            return (account, encoded);
        }

        private (string, string) FetchAccountAndGenesisJson(PodInfo pod)
        {
            var bootstrapAccount = ExecuteCommand(pod, "cat", GethDockerImage.AccountFilename);
            var bootstrapGenesisJson = ExecuteCommand(pod, "cat", GethDockerImage.GenesisFilename);
            return (bootstrapAccount, bootstrapGenesisJson);
        }

        private string ExecuteCommand(PodInfo pod, string command, params string[] arguments)
        {
            return k8sManager.ExecuteCommand(pod, K8sGethBoostrapSpecs.ContainerName, command, arguments);
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
