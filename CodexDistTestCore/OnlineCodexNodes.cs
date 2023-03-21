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
        private readonly IOnlineCodexNode[] nodes;

        public OnlineCodexNodes(IK8sManager k8SManager, IOnlineCodexNode[] nodes)
        {
            this.k8SManager = k8SManager;
            this.nodes = nodes;
        }

        public IOnlineCodexNode this[int index]
        {
            get
            {
                return nodes[index];
            }
        }

        public IOfflineCodexNodes BringOffline()
        {
            return k8SManager.BringOffline(this);
        }
    }
}
