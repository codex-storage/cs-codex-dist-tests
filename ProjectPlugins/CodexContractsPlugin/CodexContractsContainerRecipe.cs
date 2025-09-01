using CodexClient;
using GethPlugin;
using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace CodexContractsPlugin
{
    public class CodexContractsContainerRecipe : ContainerRecipeFactory
    {
        public const string DeployedAddressesFilename = "/hardhat/ignition/deployments/chain-789988/deployed_addresses.json";
        public const string MarketplaceArtifactFilename = "/hardhat/artifacts/contracts/Marketplace.sol/Marketplace.json";

        public const int PeriodSeconds = 60;
        public const int TimeoutSeconds = 30;
        public const int DowntimeSeconds = 128;

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

            // Default values:
            AddEnvVar("DISTTEST_REPAIRREWARD", 10);
            AddEnvVar("DISTTEST_MAXSLASHES", 2);
            AddEnvVar("DISTTEST_SLASHPERCENTAGE", 20);
            AddEnvVar("DISTTEST_VALIDATORREWARD", 20);
            AddEnvVar("DISTTEST_DOWNTIMEPRODUCT", 67);
            AddEnvVar("DISTTEST_MAXRESERVATIONS", 3);
            AddEnvVar("DISTTEST_MAXDURATION", Convert.ToInt32(TimeSpan.FromDays(30).TotalSeconds));

            // Customized values, required to operate in a network with
            // block frequency of 1.
            AddEnvVar("DISTTEST_PERIOD", PeriodSeconds);
            AddEnvVar("DISTTEST_TIMEOUT", TimeoutSeconds);
            AddEnvVar("DISTTEST_DOWNTIME", DowntimeSeconds);

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
