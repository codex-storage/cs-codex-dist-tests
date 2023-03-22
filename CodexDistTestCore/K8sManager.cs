namespace CodexDistTestCore
{
    public interface IK8sManager
    {
        ICodexNodeGroup BringOnline(OfflineCodexNodes node);
        IOfflineCodexNodes BringOffline(ICodexNodeGroup node);
    }

    public class K8sManager : IK8sManager
    {
        private readonly NumberSource onlineCodexNodeOrderNumberSource = new NumberSource(0);
        private readonly List<CodexNodeGroup> onlineCodexNodes = new List<CodexNodeGroup>();
        private readonly KnownK8sPods knownPods = new KnownK8sPods();
        private readonly TestLog log;
        private readonly IFileManager fileManager;

        public K8sManager(TestLog log, IFileManager fileManager)
        {
            this.log = log;
            this.fileManager = fileManager;
        }

        public ICodexNodeGroup BringOnline(OfflineCodexNodes offline)
        {
            var online = CreateOnlineCodexNodes(offline);

            K8s().BringOnline(online, offline);

            log.Log($"{online.Describe()} online.");

            return online;
        }

        public IOfflineCodexNodes BringOffline(ICodexNodeGroup node)
        {
            var online = GetAndRemoveActiveNodeFor(node);

            K8s().BringOffline(online);

            log.Log($"{online.Describe()} offline.");

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

        private CodexNodeGroup CreateOnlineCodexNodes(OfflineCodexNodes offline)
        {
            var containers = CreateContainers(offline.NumberOfNodes);
            var online = containers.Select(c => new OnlineCodexNode(log, fileManager, c)).ToArray();
            var result = new CodexNodeGroup(onlineCodexNodeOrderNumberSource.GetNextNumber(), offline, this, online);
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

        private CodexNodeGroup GetAndRemoveActiveNodeFor(ICodexNodeGroup node)
        {
            var n = (CodexNodeGroup)node;
            onlineCodexNodes.Remove(n);
            return n;
        }

        private K8sOperations K8s()
        {
            return new K8sOperations(knownPods);
        }
    }
}
