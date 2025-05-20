using BlockchainUtils;
using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using ContinuousTests;
using Core;
using GethPlugin;
using Logging;
using Utils;

namespace TraceContract
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<CodexContractsPlugin.CodexContractsPlugin>();

            var p = new Program();
            p.Run();
        }

        private readonly ILog log = new ConsoleLog();
        private readonly Input input = new();
        private readonly Config config = new();
        private readonly Output output;

        public Program()
        {
            output = new(log, input, config);
        }

        private void Run()
        {
            try
            {
                TracePurchase();
            }
            catch (Exception exc)
            {
                log.Error(exc.ToString());
            }
        }

        private void TracePurchase()
        { 
            Log("Setting up...");
            var entryPoint = new EntryPoint(log, new KubernetesWorkflow.Configuration(null, TimeSpan.FromMinutes(1.0), TimeSpan.FromSeconds(10.0), "_Unused!_"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            entryPoint.Announce();
            var ci = entryPoint.CreateInterface();
            var contracts = ConnectCodexContracts(ci);

            var chainTracer = new ChainTracer(log, contracts, input, output);
            var requestTimeRange = chainTracer.TraceChainTimeline();

            Log("Downloading storage nodes logs for the request timerange...");
            DownloadStorageNodeLogs(requestTimeRange, entryPoint.Tools);

            // package everything

            entryPoint.Decommission(false, false, false);
            Log("Done");
        }

        private ICodexContracts ConnectCodexContracts(CoreInterface ci)
        {

            var account = EthAccountGenerator.GenerateNew();
            var blockCache = new BlockCache();
            var geth = new CustomGethNode(log, blockCache, config.RpcEndpoint, config.GethPort, account.PrivateKey);

            var deployment = new CodexContractsDeployment(
                config: new MarketplaceConfig(),
                marketplaceAddress: config.MarketplaceAddress,
                abi: config.Abi,
                tokenAddress: config.TokenAddress
            );
            return ci.WrapCodexContractsDeployment(geth, deployment);
        }

        private void DownloadStorageNodeLogs(TimeRange requestTimeRange, IPluginTools tools)
        {
            var start = requestTimeRange.From - config.LogStartBeforeStorageContractStarts;

            foreach (var node in config.StorageNodesKubernetesContainerNames)
            {
                Log($"Downloading logs from '{node}'...");

                var targetFile = output.CreateNodeLogTargetFile(node);
                var downloader = new ElasticSearchLogDownloader(log, tools, config.StorageNodesKubernetesNamespace);
                downloader.Download(targetFile, node, start, requestTimeRange.To);
            }
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
