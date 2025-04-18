using GethPlugin;
using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace CodexContractsPlugin
{
    public class CodexContractsContainerRecipe : ContainerRecipeFactory
    {
        public const string MarketplaceAddressFilename = "/hardhat/deployments/codexdisttestnetwork/Marketplace.json";
        public const string MarketplaceArtifactFilename = "/hardhat/artifacts/contracts/Marketplace.sol/Marketplace.json";
        private readonly VersionRegistry versionRegistry;

        public override string AppName => "codex-contracts";
        public override string Image => versionRegistry.GetContractsDockerImage();

        public CodexContractsContainerRecipe(VersionRegistry versionRegistry)
        {
            this.versionRegistry = versionRegistry;
        }

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<CodexContractsContainerConfig>();

            var address = config.GethNode.StartResult.Container.GetAddress(GethContainerRecipe.HttpPortTag);

            SetSchedulingAffinity(notIn: "false");

            AddEnvVar("DISTTEST_NETWORK_URL", address.ToString());
            AddEnvVar("HARDHAT_NETWORK", "codexdisttestnetwork");
            AddEnvVar("KEEP_ALIVE", "1");
        }
    }
}
