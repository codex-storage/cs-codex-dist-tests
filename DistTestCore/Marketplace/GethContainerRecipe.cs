using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethContainerRecipe : ContainerRecipeFactory
    {
        public const string DockerImage = "thatbenbierens/geth-confenv:latest";
        public const string HttpPortTag = "http_port";
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
            if (config.IsBootstrapNode)
            {
                AddEnvVar("IS_BOOTSTRAP", "1");
                var exposedPort = AddExposedPort();
                return $"--http.port {exposedPort.Number}";
            }

            var port = AddInternalPort();
            var discovery = AddInternalPort();
            var authRpc = AddInternalPort();
            var httpPort = AddInternalPort(tag: HttpPortTag);
            return $"--port {port.Number} --discovery.port {discovery.Number} --authrpc.port {authRpc.Number} --http.port {httpPort.Number}";
        }
    }
}
