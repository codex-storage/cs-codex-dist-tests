using KubernetesWorkflow;

namespace GethPlugin
{
    public class GethContainerRecipe : ContainerRecipeFactory
    {
        public static string DockerImage { get; } = "codexstorage/dist-tests-geth:latest";
        private const string defaultArgs = "--ipcdisable --syncmode full";

        public const string HttpPortTag = "http_port";
        public const string DiscoveryPortTag = "disc_port";
        public const string wsPortTag = "ws_port";
        public const string AccountsFilename = "accounts.csv";

        public override string AppName => "geth";
        public override string Image => DockerImage;

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<GethStartupConfig>();

            var args = CreateArgs(config);

            AddEnvVar("GETH_ARGS", args);
        }

        private string CreateArgs(GethStartupConfig config)
        {
            var discovery = AddInternalPort(tag: DiscoveryPortTag);

            if (config.IsMiner) AddEnvVar("ENABLE_MINER", "1");
            UnlockAccounts(0, 1);
            var httpPort = AddExposedPort(tag: HttpPortTag);
            var args = $"--http.addr 0.0.0.0 --http.port {httpPort.Number} --port {discovery.Number} --discovery.port {discovery.Number} {defaultArgs}";

            var authRpc = AddInternalPort();
            var wsPort = AddInternalPort(tag: wsPortTag);

            if (config.BootstrapNode != null)
            {
                var bootPubKey = config.BootstrapNode.PublicKey;
                var bootIp = config.BootstrapNode.IpAddress;
                var bootPort = config.BootstrapNode.Port;
                var bootstrapArg = $" --bootnodes enode://{bootPubKey}@{bootIp}:{bootPort} --nat=extip:{bootIp}";
                args += bootstrapArg;
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
    }
}
