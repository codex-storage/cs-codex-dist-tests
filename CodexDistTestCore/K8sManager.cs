namespace CodexDistTestCore
{
    public interface IK8sManager
    {
        ICodexNodeGroup BringOnline(OfflineCodexNodes node);
        IOfflineCodexNodes BringOffline(ICodexNodeGroup node);
        void FetchPodLog(OnlineCodexNode node, IPodLogHandler logHandler);
    }

    public class K8sManager : IK8sManager
    {
        private readonly CodexGroupNumberSource codexGroupNumberSource = new CodexGroupNumberSource();
        private readonly List<CodexNodeGroup> onlineCodexNodeGroups = new List<CodexNodeGroup>();
        private readonly KnownK8sPods knownPods = new KnownK8sPods();
        private readonly TestLog log;
        private readonly IFileManager fileManager;
        private readonly MetricsAggregator metricsAggregator;

        public K8sManager(TestLog log, IFileManager fileManager)
        {
            this.log = log;
            this.fileManager = fileManager;
            metricsAggregator = new MetricsAggregator(log, this);
        }

        public ICodexNodeGroup BringOnline(OfflineCodexNodes offline)
        {
            var online = CreateOnlineCodexNodes(offline);

            K8s(k => k.BringOnline(online, offline));

            log.Log($"{online.Describe()} online.");

            if (offline.MetricsEnabled)
            {
                BringOnlineMetrics(online);
            }

            return online;
        }

        public IOfflineCodexNodes BringOffline(ICodexNodeGroup node)
        {
            var online = GetAndRemoveActiveNodeFor(node);

            K8s(k => k.BringOffline(online));

            log.Log($"{online.Describe()} offline.");

            return online.Origin;
        }

        public void ExampleOfCMD(IOnlineCodexNode node)
        {
            var n = (OnlineCodexNode)node;
            K8s(k => k.ExampleOfCommandExecution(n));
        }

        public void DeleteAllResources()
        {
            K8s(k => k.DeleteAllResources());
        }

        public void ForEachOnlineGroup(Action<CodexNodeGroup> action)
        {
            foreach (var group in onlineCodexNodeGroups) action(group);
        }

        public void FetchPodLog(OnlineCodexNode node, IPodLogHandler logHandler)
        {
            K8s(k => k.FetchPodLog(node, logHandler));
        }

        public PrometheusInfo BringOnlinePrometheus(string config, int prometheusNumber)
        {
            var spec = new K8sPrometheusSpecs(codexGroupNumberSource.GetNextServicePort(), prometheusNumber, config);

            PrometheusInfo? info = null;
            K8s(k => info = k.BringOnlinePrometheus(spec));
            return info!;
        }

        public void DownloadAllMetrics()
        {
            metricsAggregator.DownloadAllMetrics();
        }

        private void BringOnlineMetrics(CodexNodeGroup group)
        {
            var onlineNodes = group.Nodes.Cast<OnlineCodexNode>().ToArray();

            metricsAggregator.BeginCollectingMetricsFor(onlineNodes);
        }

        private CodexNodeGroup CreateOnlineCodexNodes(OfflineCodexNodes offline)
        {
            var containers = CreateContainers(offline);
            var online = containers.Select(c => new OnlineCodexNode(log, fileManager, c)).ToArray();
            var result = new CodexNodeGroup(log, codexGroupNumberSource.GetNextCodexNodeGroupNumber(), offline, this, online);
            onlineCodexNodeGroups.Add(result);
            return result;
        }

        private CodexNodeContainer[] CreateContainers(OfflineCodexNodes offline)
        {
            var factory = new CodexNodeContainerFactory(codexGroupNumberSource);
            var containers = new List<CodexNodeContainer>();
            for (var i = 0; i < offline.NumberOfNodes; i++) containers.Add(factory.CreateNext(offline));
            return containers.ToArray();
        }

        private CodexNodeGroup GetAndRemoveActiveNodeFor(ICodexNodeGroup node)
        {
            var n = (CodexNodeGroup)node;
            onlineCodexNodeGroups.Remove(n);
            return n;
        }

        private void K8s(Action<K8sOperations> action)
        {
            var k8s = new K8sOperations(knownPods);
            action(k8s);
            k8s.Close();
        }
    }
}
