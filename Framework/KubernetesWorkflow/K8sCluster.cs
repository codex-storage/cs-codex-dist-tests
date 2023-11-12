using k8s;

namespace KubernetesWorkflow
{
    public class K8sCluster
    {
        public K8sCluster(Configuration configuration)
        {
            Configuration = configuration;
        }

        public Configuration Configuration { get; }
        public string HostAddress { get; private set; } = string.Empty;

        public KubernetesClientConfiguration GetK8sClientConfig()
        {
            var config = GetConfig();
            UpdateHostAddress(config);
            return config;
        }

        public TimeSpan K8sOperationTimeout()
        {
            return Configuration.OperationTimeout;
        }

        public TimeSpan K8sOperationRetryDelay()
        {
            return Configuration.RetryDelay;
        }

        private KubernetesClientConfiguration GetConfig()
        {
            if (Configuration.KubeConfigFile != null)
            {
                return KubernetesClientConfiguration.BuildConfigFromConfigFile(Configuration.KubeConfigFile);
            }
            else
            {
                return KubernetesClientConfiguration.BuildDefaultConfig();
            }
        }

        private void UpdateHostAddress(KubernetesClientConfiguration config)
        {
            var host = config.Host.Replace("https://", "");
            if (host.Contains(":"))
            {
                HostAddress = "http://" + host.Substring(0, host.IndexOf(':'));
            }
            else
            {
                HostAddress = config.Host;
            }
        }
    }
}
