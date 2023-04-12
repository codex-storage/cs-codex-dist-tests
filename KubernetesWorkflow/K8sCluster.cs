using k8s;

namespace KubernetesWorkflow
{
    public class K8sCluster
    {
        public const string K8sNamespace = "codex-test-namespace";
        private const string KubeConfigFile = "C:\\kube\\config";
        private readonly Dictionary<Location, string> K8sNodeLocationMap = new Dictionary<Location, string>
        {
            { Location.BensLaptop, "worker01" },
            { Location.BensOldGamingMachine, "worker02" },
        };

        private KubernetesClientConfiguration? config;

        public KubernetesClientConfiguration GetK8sClientConfig()
        {
            if (config != null) return config;
            //config = KubernetesClientConfiguration.BuildConfigFromConfigFile(KubeConfigFile);
            config = KubernetesClientConfiguration.BuildDefaultConfig();
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
            return K8sNodeLocationMap[location];
        }
    }
}
