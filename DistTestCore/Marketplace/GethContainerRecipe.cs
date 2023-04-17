using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethContainerRecipe : ContainerRecipeFactory
    {
        public const string DockerImage = "thatbenbierens/geth-confenv:latest";
        public const string HttpPortTag = "http_port";
        public const string DiscoveryPortTag = "disc_port";
        public const string AccountFilename = "account_string.txt";
        public const string GenesisFilename = "genesis.json";
        
        protected override string Image => DockerImage;

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<GethStartupConfig>();

            var args = CreateArgs(config);

            AddEnvVar("GETH_ARGS", args);
            AddEnvVar("GENESIS_JSON", config.GenesisJsonBase64);
        }

        private string CreateArgs(GethStartupConfig config)
        {
            var discovery = AddInternalPort(tag: DiscoveryPortTag);

            if (config.IsBootstrapNode)
            {
                AddEnvVar("IS_BOOTSTRAP", "1");
                var exposedPort = AddExposedPort(tag: HttpPortTag);
                return $"--http.port {exposedPort.Number} --discovery.port {discovery.Number} --nodiscover";
            }

            var port = AddInternalPort();
            var authRpc = AddInternalPort();
            var httpPort = AddInternalPort(tag: HttpPortTag);

            var bootPubKey = config.BootstrapNode.PubKey;
            var bootIp = config.BootstrapNode.RunningContainers.Containers[0].Pod.Ip;
            var bootPort = config.BootstrapNode.DiscoveryPort.Number;
            var bootstrapArg = $"--bootnodes enode://{bootPubKey}@{bootIp}:{bootPort}";
            // geth --bootnodes enode://pubkey1@ip1:port1

            return $"--port {port.Number} --discovery.port {discovery.Number} --authrpc.port {authRpc.Number} --http.port {httpPort.Number} --nodiscover {bootstrapArg}";
        }
    }
}
