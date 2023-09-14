using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class CodexContractsContainerRecipe : DefaultContainerRecipe
    {
        public const string DockerImage = "codexstorage/dist-tests-codex-contracts-eth:sha-b4e4897";
        public const string MarketplaceAddressFilename = "/hardhat/deployments/codexdisttestnetwork/Marketplace.json";
        public const string MarketplaceArtifactFilename = "/hardhat/artifacts/contracts/Marketplace.sol/Marketplace.json";

        public override string AppName => "codex-contracts";
        public override string Image => "codexstorage/codex-contracts-eth:latest-dist-tests";

        protected override void InitializeRecipe(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<CodexContractsContainerConfig>();

            var ip = config.BootstrapNodeIp;
            var port = config.JsonRpcPort.Number;

            AddEnvVar("DISTTEST_NETWORK_URL", $"http://{ip}:{port}");
            AddEnvVar("HARDHAT_NETWORK", "codexdisttestnetwork");
            AddEnvVar("KEEP_ALIVE", "1");
        }
    }
}
