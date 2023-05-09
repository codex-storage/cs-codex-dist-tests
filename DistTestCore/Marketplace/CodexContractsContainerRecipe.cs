using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class CodexContractsContainerRecipe : ContainerRecipeFactory
    {
        #if Arm64
            public const string DockerImage = "emizzle/codex-contracts-deployment:latest";
        #else
            public const string DockerImage = "thatbenbierens/codex-contracts-deployment:nomint";
        #endif
        public const string MarketplaceAddressFilename = "/usr/app/deployments/codexdisttestnetwork/Marketplace.json";
        public const string MarketplaceArtifactFilename = "/usr/app/artifacts/contracts/Marketplace.sol/Marketplace.json";

        protected override string Image => DockerImage;

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
