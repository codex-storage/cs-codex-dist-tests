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
        public string[] AvailableK8sNodes { get; set; } = new string[0];

        public KubernetesClientConfiguration GetK8sClientConfig()
        {
            var config = GetConfig();
            UpdateHostAddress(config);
            return config;
        }

        public string GetNodeLabelForLocation(Location location)
        {
            switch (location)
            {
                case Location.One:
                    return K8sNodeIfAvailable(0);
                case Location.Two:
                    return K8sNodeIfAvailable(1);
                case Location.Three:
                    return K8sNodeIfAvailable(2);
            }
            return string.Empty;
        }

        public TimeSpan K8sOperationTimeout()
        {
            return Configuration.OperationTimeout;
        }

        public TimeSpan WaitForK8sServiceDelay()
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

        private string K8sNodeIfAvailable(int index)
        {
            if (AvailableK8sNodes.Length <= index) return string.Empty;
            return AvailableK8sNodes[index];
        }
    }
}
