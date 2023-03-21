using k8s.Models;

namespace CodexDistTestCore
{
    public interface IOnlineCodexNodes
    {
        IOfflineCodexNodes BringOffline();
        IOnlineCodexNode this[int index] { get; }
    }

    public class OnlineCodexNodes : IOnlineCodexNodes
    {
        private readonly IK8sManager k8SManager;

        public OnlineCodexNodes(int orderNumber, OfflineCodexNodes origin, IK8sManager k8SManager, OnlineCodexNode[] nodes)
        {
            OrderNumber = orderNumber;
            Origin = origin;
            this.k8SManager = k8SManager;
            Nodes = nodes;
        }

        public IOnlineCodexNode this[int index]
        {
            get
            {
                return Nodes[index];
            }
        }

        public IOfflineCodexNodes BringOffline()
        {
            return k8SManager.BringOffline(this);
        }

        public int OrderNumber { get; }
        public OfflineCodexNodes Origin { get; }
        public OnlineCodexNode[] Nodes { get; }
        public V1Deployment? Deployment { get; set; }
        public V1Service? Service { get; set; }
        public List<string> ActivePodNames { get; } = new List<string>();

        public CodexNodeContainer[] GetContainers()
        {
            return Nodes.Select(n => n.Container).ToArray();
        }

        public V1ObjectMeta GetServiceMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "codex-test-entrypoint-" + OrderNumber,
                NamespaceProperty = K8sOperations.K8sNamespace
            };
        }

        public V1ObjectMeta GetDeploymentMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "codex-test-node-" + OrderNumber,
                NamespaceProperty = K8sOperations.K8sNamespace
            };
        }

        public Dictionary<string, string> GetSelector()
        {
            return new Dictionary<string, string> { { "codex-test-node", "dist-test-" + OrderNumber } };
        }

        public string Describe()
        {
            return $"CodexNode{OrderNumber}-{Origin.Describe()}";
        }
    }
}
