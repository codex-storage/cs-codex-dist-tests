namespace CodexDistTestCore
{
    public interface IK8sManager
    {
        IOnlineCodexNodes BringOnline(OfflineCodexNodes node);
        IOfflineCodexNodes BringOffline(IOnlineCodexNodes node);
    }

    public class K8sManager : IK8sManager
    {
        private readonly NumberSource onlineCodexNodeOrderNumberSource = new NumberSource(0);
        private readonly List<OnlineCodexNodes> onlineCodexNodes = new List<OnlineCodexNodes>();
        private readonly KnownK8sPods knownPods = new KnownK8sPods();
        private readonly IFileManager fileManager;

        public K8sManager(IFileManager fileManager)
        {
            this.fileManager = fileManager;
        }

        public IOnlineCodexNodes BringOnline(OfflineCodexNodes offline)
        {
            var online = CreateOnlineCodexNodes(offline);

            K8s().BringOnline(online, offline);

            TestLog.Log($"{offline.NumberOfNodes} Codex nodes online.");

            return online;
        }

        public IOfflineCodexNodes BringOffline(IOnlineCodexNodes node)
        {
            var online = GetAndRemoveActiveNodeFor(node);

            K8s().BringOffline(online);

            TestLog.Log($"{online.Describe()} offline.");

            return online.Origin;
        }

        public void DeleteAllResources()
        {
            K8s().DeleteAllResources();
        }

        public void FetchAllPodsLogs(IPodLogsHandler logHandler)
        {
            K8s().FetchAllPodsLogs(onlineCodexNodes.ToArray(), logHandler);
        }

        private OnlineCodexNodes CreateOnlineCodexNodes(OfflineCodexNodes offline)
        {
            var containers = CreateContainers(offline.NumberOfNodes);
            var online = containers.Select(c => new OnlineCodexNode(fileManager, c)).ToArray();
            var result = new OnlineCodexNodes(onlineCodexNodeOrderNumberSource.GetNextNumber(), offline, this, online);
            onlineCodexNodes.Add(result);
            return result;
        }

        private CodexNodeContainer[] CreateContainers(int number)
        {
            var factory = new CodexNodeContainerFactory();
            var containers = new List<CodexNodeContainer>();
            for (var i = 0; i < number; i++) containers.Add(factory.CreateNext());
            return containers.ToArray();
        }

        private OnlineCodexNodes GetAndRemoveActiveNodeFor(IOnlineCodexNodes node)
        {
            var n = (OnlineCodexNodes)node;
            onlineCodexNodes.Remove(n);
            return n;
        }

        private K8sOperations K8s()
        {
            return new K8sOperations(knownPods);
        }
    }
}
