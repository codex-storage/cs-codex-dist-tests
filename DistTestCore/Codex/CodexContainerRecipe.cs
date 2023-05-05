using System.Runtime.InteropServices;
using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexContainerRecipe : ContainerRecipeFactory
    {
        #if Arm64
            public const string DockerImage = "emizzle/nim-codex-arm64:sha-c7af585";
        #else
			//public const string DockerImage = "thatbenbierens/nim-codex:sha-9716635";
            public const string DockerImage = "thatbenbierens/codexlocal:latest";
        #endif
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
                var level = config.LogLevel.ToString()!.ToUpperInvariant();
                if (config.LogTopics != null && config.LogTopics.Count() > 0){
                    level = $"INFO;{level}: {string.Join(",", config.LogTopics.Where(s => !string.IsNullOrEmpty(s)))}";
                }
                AddEnvVar("LOG_LEVEL", level);
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
            if (config.SimulateProofFailures != null)
            {
                AddEnvVar("SIMULATE_PROOF_FAILURES", config.SimulateProofFailures.ToString()!);
            }
            if (config.EnableValidator == true)
            {
                AddEnvVar("VALIDATOR", "true");
            }

            if (config.MarketplaceConfig != null || config.EnableValidator == true)
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
            if (config.MarketplaceConfig != null) {
                AddEnvVar("PERSISTENCE", "true");
            }

            if(!string.IsNullOrEmpty(config.NameOverride)) {
                AddEnvVar("CODEX_NODENAME", config.NameOverride);
            }
        }
    }
}
