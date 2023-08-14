using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexContainerRecipe : ContainerRecipeFactory
    {
        private const string DefaultDockerImage = "thatbenbierens/nim-codex:updownload";

        public const string MetricsPortTag = "metrics_port";
        public const string DiscoveryPortTag = "discovery-port";

        // Used by tests for time-constraint assersions.
        public static readonly TimeSpan MaxUploadTimePerMegabyte = TimeSpan.FromSeconds(2.0);
        public static readonly TimeSpan MaxDownloadTimePerMegabyte = TimeSpan.FromSeconds(2.0);

        public override string AppName => "codex";
        public override string Image { get; }

        public CodexContainerRecipe()
        {
            Image = GetDockerImage();
        }

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<CodexStartupConfig>();

            AddExposedPortAndVar("CODEX_API_PORT");
            AddEnvVar("CODEX_API_BINDADDR", "0.0.0.0");

            AddEnvVar("CODEX_DATA_DIR", $"datadir{ContainerNumber}");
            AddInternalPortAndVar("CODEX_DISC_PORT", DiscoveryPortTag);
            AddEnvVar("CODEX_LOG_LEVEL", config.LogLevel.ToString()!.ToUpperInvariant());

            // This makes the node announce itself to its local (pod) IP address.
            AddEnvVar("NAT_IP_AUTO", "true");

            var listenPort = AddInternalPort();
            AddEnvVar("CODEX_LISTEN_ADDRS", $"/ip4/0.0.0.0/tcp/{listenPort.Number}");

            if (!string.IsNullOrEmpty(config.BootstrapSpr))
            {
                AddEnvVar("CODEX_BOOTSTRAP_NODE", config.BootstrapSpr);
            }
            if (config.StorageQuota != null)
            {
                AddEnvVar("CODEX_STORAGE_QUOTA", config.StorageQuota.SizeInBytes.ToString()!);
            }
            if (config.BlockTTL != null)
            {
                AddEnvVar("CODEX_BLOCK_TTL", config.BlockTTL.ToString()!);
            }
            if (config.MetricsMode != Metrics.MetricsMode.None)
            {
                AddEnvVar("CODEX_METRICS", "true");
                AddEnvVar("CODEX_METRICS_ADDRESS", "0.0.0.0");
                AddInternalPortAndVar("CODEX_METRICS_PORT", tag: MetricsPortTag);
            }

            if (config.MarketplaceConfig != null)
            {
                var gethConfig = startupConfig.Get<GethStartResult>();
                var companionNode = gethConfig.CompanionNode;
                var companionNodeAccount = companionNode.Accounts[GetAccountIndex(config.MarketplaceConfig)];
                Additional(companionNodeAccount);

                var ip = companionNode.RunningContainer.Pod.PodInfo.Ip;
                var port = companionNode.RunningContainer.Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag).Number;

                AddEnvVar("CODEX_ETH_PROVIDER", $"ws://{ip}:{port}");
                AddEnvVar("CODEX_ETH_ACCOUNT", companionNodeAccount.Account);
                AddEnvVar("CODEX_MARKETPLACE_ADDRESS", gethConfig.MarketplaceNetwork.Marketplace.Address);
                AddEnvVar("CODEX_PERSISTENCE", "true");

                if (config.MarketplaceConfig.IsValidator)
                {
                    AddEnvVar("CODEX_VALIDATOR", "true");
                }
            }
        }

        private int GetAccountIndex(MarketplaceInitialConfig marketplaceConfig)
        {
            if (marketplaceConfig.AccountIndexOverride != null) return marketplaceConfig.AccountIndexOverride.Value;
            return Index;
        }

        private string GetDockerImage()
        {
            var image = Environment.GetEnvironmentVariable("CODEXDOCKERIMAGE");
            if (!string.IsNullOrEmpty(image)) return image;
            return DefaultDockerImage;
        }
    }
}
