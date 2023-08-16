using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class CodexContractsContainerRecipe : ContainerRecipeFactory
    {
        public const string DockerImage = "codexstorage/dist-tests-codex-contracts-eth:sha-ac38d7f";
        public const string MarketplaceAddressFilename = "/hardhat/deployments/codexdisttestnetwork/Marketplace.json";
        public const string MarketplaceArtifactFilename = "/hardhat/artifacts/contracts/Marketplace.sol/Marketplace.json";

        public override string AppName => "codex-contracts";
        public override string Image => "codexstorage/dist-tests-codex-contracts-eth:sha-d6fbfdc";

        protected override void Initialize(StartupConfig startupConfig)
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
