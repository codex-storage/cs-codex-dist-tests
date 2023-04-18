using KubernetesWorkflow;

namespace DistTestCore.Marketplace
{
    public class CodexContractsContainerRecipe : ContainerRecipeFactory
    {
        public const string DockerImage = "thatbenbierens/codex-contracts-deployment";
        public const string MarketplaceAddressFilename = "/usr/app/deployments/codexdisttestnetwork/Marketplace.json";

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
