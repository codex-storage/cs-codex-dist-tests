using k8s;

namespace KubernetesWorkflow
{
    public class K8sCluster
    {
        private KubernetesClientConfiguration? config;

        public K8sCluster(Configuration configuration)
        {
            Configuration = configuration;
        }
       
        public Configuration Configuration { get; }

        public KubernetesClientConfiguration GetK8sClientConfig()
        {
            if (config != null) return config;

            if (Configuration.KubeConfigFile != null)
            {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile(Configuration.KubeConfigFile);
            }
            else
            {
                config = KubernetesClientConfiguration.BuildDefaultConfig();
            }

            return config;
        }

        public string GetIp()
        {
            var c = GetK8sClientConfig();

            var host = c.Host.Replace("https://", "");

            return host.Substring(0, host.IndexOf(':'));
        }

        public string GetNodeLabelForLocation(Location location)
        {
            if (location == Location.Unspecified) return string.Empty;
            return Configuration.LocationMap.Single(l => l.Location == location).WorkerName;
        }

        public TimeSpan K8sOperationTimeout()
        {
            return Configuration.OperationTimeout;
        }

        public TimeSpan WaitForK8sServiceDelay()
        {
            return Configuration.RetryDelay;
        }
    }
}
