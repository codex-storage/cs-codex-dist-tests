using k8s;
using NUnit.Framework;

namespace CodexDistTestCore.Config
{
    public class K8sCluster
    {
        public const string K8sNamespace = "codex-test-namespace";

        public KubernetesClientConfiguration GetK8sClientConfig()
        {
            // todo: If the default KubeConfig file does not suffice, change it here:
            return KubernetesClientConfiguration.BuildConfigFromConfigFile();
        }

        public string GetIp()
        {
            return "127.0.0.1";
        }

        public string GetNodeLabelForLocation(Location location)
        {
            switch (location)
            {
                case Location.Unspecified:
                    return string.Empty;
                case Location.BensLaptop:
                    return "worker01";
                case Location.BensOldGamingMachine:
                    return "worker02";
            }

            Assert.Fail("Unknown location selected: " + location);
            throw new InvalidOperationException();
        }
    }
}
