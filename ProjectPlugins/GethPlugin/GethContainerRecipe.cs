using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace GethPlugin
{
    public class GethContainerRecipe : ContainerRecipeFactory
    {
        public static string DockerImage { get; } = "codexstorage/dist-tests-geth:latest";
        private const string defaultArgs = "--ipcdisable --syncmode full";

        public const string HttpPortTag = "http_port";
        public const string DiscoveryPortTag = "disc_port";
        public const string ListenPortTag = "listen_port";
        public const string WsPortTag = "ws_port";
        public const string AuthRpcPortTag = "auth_rpc_port";
        public const string AccountsFilename = "accounts.csv";

        public override string AppName => "geth";
        public override string Image => DockerImage;

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<GethStartupConfig>();

            var args = CreateArgs(config);

            SetSchedulingAffinity(notIn: "tests-runners");

            AddEnvVar("GETH_ARGS", args);
        }

        private string CreateArgs(GethStartupConfig config)
        {
            if (config.IsMiner) AddEnvVar("ENABLE_MINER", "1");
            UnlockAccounts(0, 1);

            var httpPort = CreateApiPort(config, tag: HttpPortTag);
            var discovery = CreateDiscoveryPort(config);
            var listen = CreateListenPort(config);
            var authRpc = CreateP2pPort(config, tag: AuthRpcPortTag);
            var wsPort = CreateP2pPort(config, tag: WsPortTag);

            var args = $"--http.addr 0.0.0.0 --http.port {httpPort.Number} --port {listen.Number} --discovery.port {discovery.Number} {defaultArgs}";

            if (config.BootstrapNode != null)
            {
                var bootPubKey = config.BootstrapNode.PublicKey;
                var bootIp = config.BootstrapNode.IpAddress;
                var bootPort = config.BootstrapNode.Port;
                var bootstrapArg = $" --bootnodes enode://{bootPubKey}@{bootIp}:{bootPort} --nat=extip:{bootIp}";
                args += bootstrapArg;
            }
            if (config.IsPublicTestNet != null)
            {
                AddEnvVar("NAT_PUBLIC_IP_AUTO", "true");
            }
            else
            {
                AddEnvVar("NAT_PUBLIC_IP_AUTO", "false");
            }

            return args + $" --authrpc.port {authRpc.Number} --ws --ws.addr 0.0.0.0 --ws.port {wsPort.Number}";
        }

        private void UnlockAccounts(int startIndex, int numberOfAccounts)
        {
            if (startIndex < 0) throw new ArgumentException();
            if (numberOfAccounts < 1) throw new ArgumentException();
            if (startIndex + numberOfAccounts > 1000) throw new ArgumentException("Out of accounts!");

            AddEnvVar("UNLOCK_START_INDEX", startIndex.ToString());
            AddEnvVar("UNLOCK_NUMBER", numberOfAccounts.ToString());
        }

        private Port CreateDiscoveryPort(GethStartupConfig config)
        {
            if (config.IsPublicTestNet == null) return AddInternalPort(DiscoveryPortTag);

            return AddExposedPort(config.IsPublicTestNet.DiscoveryPort, DiscoveryPortTag, PortProtocol.UDP);
        }

        private Port CreateListenPort(GethStartupConfig config)
        {
            if (config.IsPublicTestNet == null) return AddInternalPort(ListenPortTag);

            return AddExposedPort(config.IsPublicTestNet.ListenPort, ListenPortTag);
        }

        private Port CreateP2pPort(GethStartupConfig config, string tag)
        {
            if (config.IsPublicTestNet != null)
            {
                return AddExposedPort(tag);
            }
            return AddInternalPort(tag);
        }

        private Port CreateApiPort(GethStartupConfig config, string tag)
        {
            if (config.IsPublicTestNet != null)
            {
                return AddInternalPort(tag);
            }
            return AddExposedPort(tag);
        }
    }
}
