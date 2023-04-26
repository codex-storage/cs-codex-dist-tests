using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexContainerRecipe : ContainerRecipeFactory
    {
        //public const string DockerImage = "thatbenbierens/nim-codex:sha-bf5512b";
        public const string DockerImage = "thatbenbierens/codexlocal:latest";
        public const string MetricsPortTag = "metrics_port";

        protected override string Image => DockerImage;

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<CodexStartupConfig>();

            AddExposedPortAndVar("API_PORT");
            AddEnvVar("DATA_DIR", $"datadir{ContainerNumber}");
            AddInternalPortAndVar("DISC_PORT");

            var listenPort = AddInternalPort();
            AddEnvVar("LISTEN_ADDRS", $"/ip4/0.0.0.0/tcp/{listenPort.Number}");

            if (!string.IsNullOrEmpty(config.BootstrapSpr))
            {
                AddEnvVar("BOOTSTRAP_SPR", config.BootstrapSpr);
            }

            if (config.LogLevel != null)
            {
                AddEnvVar("LOG_LEVEL", config.LogLevel.ToString()!.ToUpperInvariant());
            }
            if (config.StorageQuota != null)
            {
                AddEnvVar("STORAGE_QUOTA", config.StorageQuota.SizeInBytes.ToString()!);
            }
            if (config.MetricsEnabled)
            {
                AddEnvVar("METRICS_ADDR", "0.0.0.0");
                AddInternalPortAndVar("METRICS_PORT", tag: MetricsPortTag);
            }

            if (config.MarketplaceConfig != null)
            {
                var gethConfig = startupConfig.Get<GethStartResult>();
                var companionNode = gethConfig.CompanionNode;
                var companionNodeAccount = companionNode.Accounts[Index];
                Additional(companionNodeAccount);

                var ip = companionNode.RunningContainer.Pod.Ip;
                var port = companionNode.RunningContainer.Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag).Number;

                AddEnvVar("ETH_PROVIDER", $"ws://{ip}:{port}");
                AddEnvVar("ETH_ACCOUNT", companionNodeAccount.Account);
                AddEnvVar("ETH_MARKETPLACE_ADDRESS", gethConfig.MarketplaceNetwork.Marketplace.Address);
            }
        }
    }
}
