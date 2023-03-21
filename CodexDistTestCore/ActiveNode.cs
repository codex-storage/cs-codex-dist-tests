using k8s.Models;

namespace CodexDistTestCore
{
    public class ActiveNode
    {
        public ActiveNode(OfflineCodexNode origin, int port, int orderNumber)
        {
            Origin = origin;
            SelectorName = orderNumber.ToString().PadLeft(6, '0');
            Port = port;
        }

        public OfflineCodexNode Origin { get; }
        public string SelectorName { get; }
        public int Port { get; }
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

        public string GetContainerPortName()
        {
            //Caution, was: "codex-api-port" + SelectorName
            //but string length causes 'UnprocessableEntity' exception in k8s.
            return "api-" + SelectorName;
        }

        public string GetContainerName()
        {
            return "codex-test-node";
        }

        public string Describe()
        {
            return $"CodexNode{SelectorName}-Port:{Port}-{Origin.Describe()}";
        }
    }
}
