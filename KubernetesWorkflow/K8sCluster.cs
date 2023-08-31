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
        public K8sNodeLabel[] AvailableK8sNodes { get; set; } = new K8sNodeLabel[0];

        public KubernetesClientConfiguration GetK8sClientConfig()
        {
            var config = GetConfig();
            UpdateHostAddress(config);
            return config;
        }

        public K8sNodeLabel? GetNodeLabelForLocation(Location location)
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
            return null;
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

        private K8sNodeLabel? K8sNodeIfAvailable(int index)
        {
            if (AvailableK8sNodes.Length <= index) return null;
            return AvailableK8sNodes[index];
        }
    }

    public class K8sNodeLabel
    {
        public K8sNodeLabel(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Value { get; }
    }
}
