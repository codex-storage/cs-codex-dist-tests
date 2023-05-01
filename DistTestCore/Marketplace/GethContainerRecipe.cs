﻿using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class GethContainerRecipe : ContainerRecipeFactory
    {
        public const string DockerImage = "thatbenbierens/geth-confenv:latest";
        public const string HttpPortTag = "http_port";
        public const string DiscoveryPortTag = "disc_port";
        private const string defaultArgs = "--ipcdisable --syncmode full";

        public static string GetAccountFilename(int? orderNumber)
        {
            if (orderNumber == null) return "account_string.txt";
            return $"account_string_{orderNumber.Value}.txt";
        }

        public static string GetPrivateKeyFilename(int? orderNumber)
        {
            if (orderNumber == null) return "private.key";
            return $"private_{orderNumber.Value}.key";
        }

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
            return $"--http.port {exposedPort.Number} --port {discovery.Number} --discovery.port {discovery.Number} {defaultArgs}";
        }

        private string CreateCompanionArgs(Port discovery, GethStartupConfig config)
        {
            AddEnvVar("NUMBER_OF_ACCOUNTS", config.NumberOfCompanionAccounts.ToString());

            var port = AddInternalPort();
            var authRpc = AddInternalPort();
            var httpPort = AddExposedPort(tag: HttpPortTag);

            var bootPubKey = config.BootstrapNode.PubKey;
            var bootIp = config.BootstrapNode.RunningContainers.Containers[0].Pod.Ip;
            var bootPort = config.BootstrapNode.DiscoveryPort.Number;
            var bootstrapArg = $"--bootnodes enode://{bootPubKey}@{bootIp}:{bootPort} --nat=extip:{bootIp}";

            return $"--port {port.Number} --discovery.port {discovery.Number} --authrpc.port {authRpc.Number} --http.addr 0.0.0.0 --http.port {httpPort.Number} --ws --ws.addr 0.0.0.0 --ws.port {httpPort.Number} {bootstrapArg} {defaultArgs}";
        }
    }
}
