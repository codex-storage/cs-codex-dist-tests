using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexContainerRecipe : DefaultContainerRecipe
    {
        private const string DefaultDockerImage = "codexstorage/nim-codex:latest-dist-tests";

        public const string MetricsPortTag = "metrics_port";
        public const string DiscoveryPortTag = "discovery-port";

        // Used by tests for time-constraint assertions.
        public static readonly TimeSpan MaxUploadTimePerMegabyte = TimeSpan.FromSeconds(2.0);
        public static readonly TimeSpan MaxDownloadTimePerMegabyte = TimeSpan.FromSeconds(2.0);

        public override string AppName => "codex";
        public override string Image { get; }

        public CodexContainerRecipe()
        {
            Image = GetDockerImage();
        }

        protected override void InitializeRecipe(StartupConfig startupConfig)
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
            if (config.BlockMaintenanceInterval != null)
            {
                AddEnvVar("CODEX_BLOCK_MI", Convert.ToInt32(config.BlockMaintenanceInterval.Value.TotalSeconds).ToString());
            }
            if (config.BlockMaintenanceNumber != null)
            {
                AddEnvVar("CODEX_BLOCK_MN", config.BlockMaintenanceNumber.ToString()!);
            }
            if (config.MetricsMode != Metrics.MetricsMode.None)
            {
                var metricsPort = AddInternalPort(MetricsPortTag);
                AddEnvVar("CODEX_METRICS", "true");
                AddEnvVar("CODEX_METRICS_ADDRESS", "0.0.0.0");
                AddEnvVar("CODEX_METRICS_PORT", metricsPort);
                AddPodAnnotation("prometheus.io/scrape", "true");
                AddPodAnnotation("prometheus.io/port", metricsPort.Number.ToString());
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
