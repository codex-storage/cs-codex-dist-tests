using CodexDistTestCore.Marketplace;
using CodexDistTestCore.Metrics;

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
        private readonly MarketplaceController marketplaceController;

        public K8sManager(TestLog log, IFileManager fileManager)
        {
            this.log = log;
            this.fileManager = fileManager;
            metricsAggregator = new MetricsAggregator(log, this);
            marketplaceController = new MarketplaceController(log, this);
        }

        public ICodexNodeGroup BringOnline(OfflineCodexNodes offline)
        {
            var group = CreateOnlineCodexNodes(offline);

            if (offline.MarketplaceConfig != null)
            {
                group.GethCompanionGroup = marketplaceController.BringOnlineMarketplace(offline);
                ConnectMarketplace(group);
            }

            K8s(k => k.BringOnline(group, offline));

            if (offline.MetricsEnabled)
            {
                BringOnlineMetrics(group);
            }

            log.Log($"{group.Describe()} online.");

            return group;
        }

        public IOfflineCodexNodes BringOffline(ICodexNodeGroup node)
        {
            var online = GetAndRemoveActiveNodeFor(node);

            K8s(k => k.BringOffline(online));

            log.Log($"{online.Describe()} offline.");

            return online.Origin;
        }

        public string ExecuteCommand(PodInfo pod, string containerName, string command, params string[] arguments)
        {
            return K8s(k => k.ExecuteCommand(pod, containerName, command, arguments));
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

            return K8s(k => k.BringOnlinePrometheus(spec));
        }

        public K8sGethBoostrapSpecs CreateGethBootstrapNodeSpec()
        {
            return new K8sGethBoostrapSpecs(codexGroupNumberSource.GetNextServicePort());
        }

        public PodInfo BringOnlineGethBootstrapNode(K8sGethBoostrapSpecs spec)
        {
            return K8s(k => k.BringOnlineGethBootstrapNode(spec));
        }

        public PodInfo BringOnlineGethCompanionGroup(GethBootstrapInfo info, GethCompanionGroup group)
        {
            return K8s(k => k.BringOnlineGethCompanionGroup(info, group));
        }

        public void DownloadAllMetrics()
        {
            metricsAggregator.DownloadAllMetrics();
        }

        private void BringOnlineMetrics(CodexNodeGroup group)
        {
            metricsAggregator.BeginCollectingMetricsFor(DowncastNodes(group));
        }

        private void ConnectMarketplace(CodexNodeGroup group)
        {
            for (var i = 0; i < group.Nodes.Length; i++)
            {
                ConnectMarketplace(group, group.Nodes[i], group.GethCompanionGroup!.Containers[i]);
            }
        }

        private void ConnectMarketplace(CodexNodeGroup group, OnlineCodexNode node, GethCompanionNodeContainer container)
        {
            node.Container.GethCompanionNodeContainer = container; // :c

            var access = new MarketplaceAccess(this, marketplaceController, log, group, container);
            access.Initialize();
            node.Marketplace = access;
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

        private T K8s<T>(Func<K8sOperations, T> action)
        {
            var k8s = new K8sOperations(knownPods);
            var result = action(k8s);
            k8s.Close();
            return result;
        }

        private static OnlineCodexNode[] DowncastNodes(CodexNodeGroup group)
        {
            return group.Nodes.Cast<OnlineCodexNode>().ToArray();
        }
    }
}
