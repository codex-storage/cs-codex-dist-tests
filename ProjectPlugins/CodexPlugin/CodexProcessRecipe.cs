using System.Net.Sockets;
using System.Net;
using Utils;
using Nethereum.Util;

namespace CodexPlugin
{
    public class ProcessRecipe
    {
        public ProcessRecipe(string cmd, string[] args)
        {
            Cmd = cmd;
            Args = args;
        }

        public string Cmd { get; }
        public string[] Args { get; }
    }

    public class CodexPortMap
    {
        public CodexPortMap(FreePortFinder freePortFinder)
        {
            ApiPort = freePortFinder.GetNextFreePort();
            DiscPort = freePortFinder.GetNextFreePort();
            ListenPort = freePortFinder.GetNextFreePort();
        }

        public int ApiPort { get; }
        public int DiscPort { get; }
        public int ListenPort { get; }
    }

    public class CodexProcessRecipe
    {
        private readonly CodexPortMap portMap;
        private readonly NumberSource numberSource;

        public CodexProcessRecipe(CodexPortMap portMap, NumberSource numberSource)
        {
            this.portMap = portMap;
            this.numberSource = numberSource;
        }

        public ProcessRecipe Initialize(CodexStartupConfig config)
        {
            args.Clear();
            
            AddArg("--api-port", portMap.ApiPort);
            AddArg("--api-bindaddr", "0.0.0.0");

            var dataDir = $"datadir_{numberSource.GetNextNumber()}";
            AddArg("--data-dir", dataDir);

            AddArg("--disc-port", portMap.DiscPort);
            AddArg("--log-level", config.LogLevelWithTopics());

            // This makes the node announce itself to its local IP address.
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addrs = host.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToList();
            var ipaddrs = addrs.First();

            AddArg("--nat", $"extip:{ipaddrs.ToStringInvariant()}");
            
            AddArg("--listen-addrs", $"/ip4/0.0.0.0/tcp/{portMap.ListenPort}");

            if (!string.IsNullOrEmpty(config.BootstrapSpr))
            {
                AddArg("--bootstrap-node", config.BootstrapSpr);
            }
            if (config.StorageQuota != null)
            {
                AddArg("--storage-quota", config.StorageQuota.SizeInBytes.ToString()!);
            }
            if (config.BlockTTL != null)
            {
                AddArg("--block-ttl", config.BlockTTL.ToString()!);
            }
            if (config.BlockMaintenanceInterval != null)
            {
                AddArg("--block-mi", Convert.ToInt32(config.BlockMaintenanceInterval.Value.TotalSeconds).ToString());
            }
            if (config.BlockMaintenanceNumber != null)
            {
                AddArg("--block-mn", config.BlockMaintenanceNumber.ToString()!);
            }
            if (config.MetricsEnabled)
            {
                throw new Exception("Not supported");
                //var metricsPort = CreateApiPort(config, MetricsPortTag);
                //AddEnvVar("CODEX_METRICS", "true");
                //AddEnvVar("CODEX_METRICS_ADDRESS", "0.0.0.0");
                //AddEnvVar("CODEX_METRICS_PORT", metricsPort);
                //AddPodAnnotation("prometheus.io/scrape", "true");
                //AddPodAnnotation("prometheus.io/port", metricsPort.Number.ToString());
            }

            if (config.SimulateProofFailures != null)
            {
                throw new Exception("Not supported");
                //AddEnvVar("CODEX_SIMULATE_PROOF_FAILURES", config.SimulateProofFailures.ToString()!);
            }

            if (config.MarketplaceConfig != null)
            {
                throw new Exception("Not supported");
                //var mconfig = config.MarketplaceConfig;
                //var gethStart = mconfig.GethNode.StartResult;
                //var wsAddress = gethStart.Container.GetInternalAddress(GethContainerRecipe.WsPortTag);
                //var marketplaceAddress = mconfig.CodexContracts.Deployment.MarketplaceAddress;

                //AddEnvVar("CODEX_ETH_PROVIDER", $"{wsAddress.Host.Replace("http://", "ws://")}:{wsAddress.Port}");
                //AddEnvVar("CODEX_MARKETPLACE_ADDRESS", marketplaceAddress);

                //var marketplaceSetup = config.MarketplaceConfig.MarketplaceSetup;

                //// Custom scripting in the Codex test image will write this variable to a private-key file,
                //// and pass the correct filename to Codex.
                //var account = marketplaceSetup.EthAccountSetup.GetNew();
                //AddEnvVar("ETH_PRIVATE_KEY", account.PrivateKey);
                //Additional(account);

                //SetCommandOverride(marketplaceSetup);
                //if (marketplaceSetup.IsValidator)
                //{
                //    AddEnvVar("CODEX_VALIDATOR", "true");
                //}
            }

            //if (!string.IsNullOrEmpty(config.NameOverride))
            //{
            //    AddEnvVar("CODEX_NODENAME", config.NameOverride);
            //}

            return Create();
        }

        private ProcessRecipe Create()
        {
            return new ProcessRecipe(
                cmd: Path.Combine(
                    "d:",
                    "Dev",
                    "nim-codex",
                    "build",
                    "codex.exe"
                ),
                args: args.ToArray());
        }

        private readonly List<string> args = new List<string>();

        private void AddArg(string arg, string val)
        {
            args.Add($"{arg}={val}");
        }

        private void AddArg(string arg, int val)
        {
            args.Add($"{arg}={val}");
        }
    }
}
