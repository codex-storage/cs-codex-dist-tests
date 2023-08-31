using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethContainerRecipe : DefaultContainerRecipe
    {
        private const string defaultArgs = "--ipcdisable --syncmode full";

        public const string HttpPortTag = "http_port";
        public const string DiscoveryPortTag = "disc_port";
        public const string AccountsFilename = "accounts.csv";

        public override string AppName => "geth";
        public override string Image => "codexstorage/dist-tests-geth:latest";

        protected override void InitializeRecipe(StartupConfig startupConfig)
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
            AddEnvVar("ENABLE_MINER", "1");
            UnlockAccounts(0, 1);
            var exposedPort = AddExposedPort(tag: HttpPortTag);
            return $"--http.port {exposedPort.Number} --port {discovery.Number} --discovery.port {discovery.Number} {defaultArgs}";
        }

        private string CreateCompanionArgs(Port discovery, GethStartupConfig config)
        {
            UnlockAccounts(
                config.CompanionAccountStartIndex + 1,
                config.NumberOfCompanionAccounts);

            var port = AddInternalPort();
            var authRpc = AddInternalPort();
            var httpPort = AddExposedPort(tag: HttpPortTag);

            var bootPubKey = config.BootstrapNode.PubKey;
            var bootIp = config.BootstrapNode.RunningContainers.Containers[0].Pod.PodInfo.Ip;
            var bootPort = config.BootstrapNode.DiscoveryPort.Number;
            var bootstrapArg = $"--bootnodes enode://{bootPubKey}@{bootIp}:{bootPort} --nat=extip:{bootIp}";

            return $"--port {port.Number} --discovery.port {discovery.Number} --authrpc.port {authRpc.Number} --http.addr 0.0.0.0 --http.port {httpPort.Number} --ws --ws.addr 0.0.0.0 --ws.port {httpPort.Number} {bootstrapArg} {defaultArgs}";
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
