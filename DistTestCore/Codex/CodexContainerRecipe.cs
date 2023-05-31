using System.Runtime.InteropServices;
using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexContainerRecipe : ContainerRecipeFactory
    {
        #if Arm64
            public const string DockerImage = "codexstorage/nim-codex:sha-7b88ea0";
        #else
			//public const string DockerImage = "codexstorage/nim-codex:sha-7b88ea0";
            public const string DockerImage = "codexstorage/nim-codex:sha-7b88ea0";
        #endif
        public const string MetricsPortTag = "metrics_port";
        public const string DiscoveryPortTag = "discovery-port";

        protected override string Image => DockerImage;

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<CodexStartupConfig>();

            AddExposedPortAndVar("API_PORT");
            AddEnvVar("DATA_DIR", $"datadir{ContainerNumber}");
            AddInternalPortAndVar("DISC_PORT", DiscoveryPortTag);
            AddEnvVar("LOG_LEVEL", config.LogLevel.ToString()!.ToUpperInvariant());

            var listenPort = AddInternalPort();
            AddEnvVar("LISTEN_ADDRS", $"/ip4/0.0.0.0/tcp/{listenPort.Number}");

            if (!string.IsNullOrEmpty(config.BootstrapSpr))
            {
                AddEnvVar("BOOTSTRAP_SPR", config.BootstrapSpr);
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
