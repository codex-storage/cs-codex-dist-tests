using Core;
using KubernetesWorkflow.Types;
using MetricsPlugin;
using System.Collections;

namespace CodexPlugin
{
    public interface ICodexNodeGroup : IEnumerable<ICodexNode>, IHasManyMetricScrapeTargets
    {
        void BringOffline(bool waitTillStopped);
        ICodexNode this[int index] { get; }
    }

    public class CodexNodeGroup : ICodexNodeGroup
    {
        private readonly CodexStarter starter;

        public CodexNodeGroup(CodexStarter starter, IPluginTools tools, RunningPod[] containers, ICodexNodeFactory codexNodeFactory)
        {
            this.starter = starter;
            Containers = containers;
            Nodes = containers.Select(c => CreateOnlineCodexNode(c, tools, codexNodeFactory)).ToArray();
            Version = new DebugInfoVersion();
        }

        public ICodexNode this[int index]
        {
            get
            {
                return Nodes[index];
            }
        }

        public void BringOffline(bool waitTillStopped)
        {
            starter.BringOffline(this, waitTillStopped);
            // Clear everything. Prevent accidental use.
            Nodes = Array.Empty<CodexNode>();
            Containers = null!;
        }

        public void Stop(CodexNode node, bool waitTillStopped)
        {
            starter.Stop(node.Pod, waitTillStopped);
            Nodes = Nodes.Where(n => n != node).ToArray();
            Containers = Containers.Where(c => c != node.Pod).ToArray();
        }

        public RunningPod[] Containers { get; private set; }
        public CodexNode[] Nodes { get; private set; }
        public DebugInfoVersion Version { get; private set; }
        public IMetricsScrapeTarget[] ScrapeTargets => Nodes.Select(n => n.MetricsScrapeTarget).ToArray();

        public IEnumerator<ICodexNode> GetEnumerator()
        {
            return Nodes.Cast<ICodexNode>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        public string Describe()
        {
            return $"group:[{Containers.Describe()}]";
        }

        public void EnsureOnline()
        {
            foreach (var node in Nodes) node.EnsureOnlineGetVersionResponse();
            var versionResponses = Nodes.Select(n => n.Version);

            var first = versionResponses.First();
            if (!versionResponses.All(v => v.Version == first.Version && v.Revision == first.Revision))
            {
                throw new Exception("Inconsistent version information received from one or more Codex nodes: " +
                    string.Join(",", versionResponses.Select(v => v.ToString())));
            }

            Version = first;
        }

        private CodexNode CreateOnlineCodexNode(RunningPod c, IPluginTools tools, ICodexNodeFactory factory)
        {
            var watcher = factory.CreateCrashWatcher(c.Containers.Single());
            var access = new CodexAccess(tools, c, watcher);
            return factory.CreateOnlineCodexNode(access, this);
        }
    }
}
