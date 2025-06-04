using CodexClient;
using GethPlugin;
using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace CodexContractsPlugin
{
    public class CodexContractsContainerRecipe : ContainerRecipeFactory
    {
        public const string MarketplaceAddressFilename = "/hardhat/deployments/codexdisttestnetwork/Marketplace.json";
        public const string MarketplaceArtifactFilename = "/hardhat/artifacts/contracts/Marketplace.sol/Marketplace.json";
        private readonly DebugInfoVersion versionInfo;

        public override string AppName => "codex-contracts";
        public override string Image => GetContractsDockerImage();

        public CodexContractsContainerRecipe(DebugInfoVersion versionInfo)
        {
            this.versionInfo = versionInfo;
        }

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<CodexContractsContainerConfig>();

            var address = config.GethNode.StartResult.Container.GetAddress(GethContainerRecipe.HttpPortTag);

            SetSchedulingAffinity(notIn: "false");

            AddEnvVar("DISTTEST_NETWORK_URL", address.ToString());
            AddEnvVar("HARDHAT_NETWORK", "codexdisttestnetwork");
            AddEnvVar("HARDHAT_IGNITION_CONFIRM_DEPLOYMENT", "false");
            AddEnvVar("KEEP_ALIVE", "1");
        }

        private string GetContractsDockerImage()
        {
            return $"codexstorage/codex-contracts-eth:sha-{versionInfo.Contracts}-dist-tests";
        }
    }
}
