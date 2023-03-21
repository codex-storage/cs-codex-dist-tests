using k8s.Models;

namespace CodexDistTestCore
{
    public class ActiveDeployment
    {
        public ActiveDeployment(OfflineCodexNodes origin, int orderNumber, CodexNodeContainer[] containers)
        {
            Origin = origin;
            Containers = containers;
            SelectorName = orderNumber.ToString().PadLeft(6, '0');
        }

        public OfflineCodexNodes Origin { get; }
        public CodexNodeContainer[] Containers { get; }
        public string SelectorName { get; }
        public V1Deployment? Deployment { get; set; }
        public V1Service? Service { get; set; }
        public List<string> ActivePodNames { get; } = new List<string>();

        public V1ObjectMeta GetServiceMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "codex-test-entrypoint-" + SelectorName,
                NamespaceProperty = K8sManager.K8sNamespace
            };
        }

        public V1ObjectMeta GetDeploymentMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "codex-test-node-" + SelectorName,
                NamespaceProperty = K8sManager.K8sNamespace
            };
        }

        public Dictionary<string, string> GetSelector()
        {
            return new Dictionary<string, string> { { "codex-test-node", "dist-test-" + SelectorName } };
        }

        public string Describe()
        {
            return $"CodexNode{SelectorName}-{Origin.Describe()}";
        }
    }
}
