using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class CodexContractsContainerRecipe : ContainerRecipeFactory
    {
        public const string MarketplaceAddressFilename = "/usr/app/deployments/codexdisttestnetwork/Marketplace.json";
        public const string MarketplaceArtifactFilename = "/usr/app/artifacts/contracts/Marketplace.sol/Marketplace.json";

        public override string Image { get; }

        public CodexContractsContainerRecipe()
        {
#if Arm64
            Image = "emizzle/codex-contracts-deployment:latest";
#else
            Image = "thatbenbierens/codex-contracts-deployment:nomint2";
#endif
        }

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
