﻿using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexContainerRecipe : ContainerRecipeFactory
    {
        public const string MetricsPortTag = "metrics_port";

        protected override string Image => "thatbenbierens/nim-codex:sha-b204837";

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<CodexStartupConfig>();

            AddExposedPortAndVar("API_PORT");
            AddEnvVar("DATA_DIR", $"datadir{ContainerNumber}");
            AddInternalPortAndVar("DISC_PORT");

            var listenPort = AddInternalPort();
            AddEnvVar("LISTEN_ADDRS", $"/ip4/0.0.0.0/tcp/{listenPort.Number}");

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
                var companionNode = gethConfig.CompanionNodes[Index];

                // Bootstrap node access from within the cluster:
                //var ip = gethConfig.BootstrapNode.RunningContainers.RunningPod.Ip;
                //var port = gethConfig.BootstrapNode.RunningContainers.Containers[0].Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag);

                //AddEnvVar("ETH_PROVIDER", "todo");
                //AddEnvVar("ETH_ACCOUNT", companionNode.Account);
                //AddEnvVar("ETH_DEPLOYMENT", "todo");
            }
        }
    }
}
