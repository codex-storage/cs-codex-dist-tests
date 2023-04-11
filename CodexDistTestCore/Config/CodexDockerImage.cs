using k8s.Models;

namespace CodexDistTestCore.Config
{
    public class CodexDockerImage
    {
        public string GetImageTag()
        {
            return "thatbenbierens/nim-codex:sha-b204837";
        }

        public string GetExpectedImageRevision()
        {
            return "b20483";
        }

        public List<V1EnvVar> CreateEnvironmentVariables(OfflineCodexNodes node, CodexNodeContainer container)
        {
            var formatter = new EnvFormatter();
            formatter.Create(node, container);
            return formatter.Result;
        }

        private class EnvFormatter
        {
            public List<V1EnvVar> Result { get; } = new List<V1EnvVar>();

            public void Create(OfflineCodexNodes node, CodexNodeContainer container)
            {
                AddVar("API_PORT", container.ApiPort.ToString());
                AddVar("DATA_DIR", container.DataDir);
                AddVar("DISC_PORT", container.DiscoveryPort.ToString());
                AddVar("LISTEN_ADDRS", $"/ip4/0.0.0.0/tcp/{container.ListenPort}");

                if (node.BootstrapNode != null)
                {
                    var debugInfo = node.BootstrapNode.GetDebugInfo();
                    AddVar("BOOTSTRAP_SPR", debugInfo.spr);
                }
                if (node.LogLevel != null)
                {
                    AddVar("LOG_LEVEL", node.LogLevel.ToString()!.ToUpperInvariant());
                }
                if (node.StorageQuota != null)
                {
                    AddVar("STORAGE_QUOTA", node.StorageQuota.SizeInBytes.ToString()!);
                }
                if (node.MetricsEnabled)
                {
                    AddVar("METRICS_ADDR", "0.0.0.0");
                    AddVar("METRICS_PORT", container.MetricsPort.ToString());
                }
                if (node.MarketplaceConfig != null)
                {
                    //ETH_PROVIDER
                    //ETH_ACCOUNT
                    //ETH_DEPLOYMENT
                    AddVar("ETH_ACCOUNT", container.GethCompanionNodeContainer!.Account);
                }
            }

            private void AddVar(string key, string value)
            {
                Result.Add(new V1EnvVar
                {
                    Name = key,
                    Value = value
                });
            }
        }
    }
}
