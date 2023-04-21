using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethContainerRecipe : ContainerRecipeFactory
    {
        public const string DockerImage = "thatbenbierens/geth-confenv:latest";
        public const string HttpPortTag = "http_port";
        public const string WsPortTag = "ws_port";
        public const string DiscoveryPortTag = "disc_port";
        public const string AccountFilename = "account_string.txt";
        public const string BootstrapPrivateKeyFilename = "bootstrap_private.key";

        protected override string Image => DockerImage;

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<GethStartupConfig>();

            var args = CreateArgs(config);

            AddEnvVar("GETH_ARGS", args);
        }

        private string CreateArgs(GethStartupConfig config)
        {
            var discovery = AddInternalPort(tag: DiscoveryPortTag);

            if (config.IsBootstrapNode)
            {
                return CreateBootstapArgs(discovery);
            }

            return CreateCompanionArgs(discovery, config);
        }

        private string CreateBootstapArgs(Port discovery)
        {
            AddEnvVar("IS_BOOTSTRAP", "1");
            var exposedPort = AddExposedPort(tag: HttpPortTag);
            return $"--http.port {exposedPort.Number} --port {discovery.Number} --discovery.port {discovery.Number}";
        }

        private string CreateCompanionArgs(Port discovery, GethStartupConfig config)
        {
            var port = AddInternalPort();
            var authRpc = AddInternalPort();
            var httpPort = AddInternalPort(tag: HttpPortTag);
            var wsPort = AddInternalPort(tag: WsPortTag);

            var bootPubKey = config.BootstrapNode.PubKey;
            var bootIp = config.BootstrapNode.RunningContainers.Containers[0].Pod.Ip;
            var bootPort = config.BootstrapNode.DiscoveryPort.Number;
            var bootstrapArg = $"--bootnodes enode://{bootPubKey}@{bootIp}:{bootPort}";

            return $"--port {port.Number} --discovery.port {discovery.Number} --authrpc.port {authRpc.Number} --http.port {httpPort.Number} --ws --ws.addr 0.0.0.0 --ws.port {wsPort.Number} {bootstrapArg}";
        }
    }
}
