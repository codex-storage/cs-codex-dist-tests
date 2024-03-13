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

        public CodexNodeGroup(CodexStarter starter, IPluginTools tools, RunningContainers[] containers, ICodexNodeFactory codexNodeFactory)
        {
            this.starter = starter;
            Containers = containers;
            Nodes = containers.Containers().Select(c => CreateOnlineCodexNode(c, tools, codexNodeFactory)).ToArray();
            Version = new CodexDebugVersionResponse();
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

        public RunningContainers[] Containers { get; private set; }
        public CodexNode[] Nodes { get; private set; }
        public CodexDebugVersionResponse Version { get; private set; }
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
            if (!versionResponses.All(v => v.version == first.version && v.revision == first.revision))
            {
                throw new Exception("Inconsistent version information received from one or more Codex nodes: " +
                    string.Join(",", versionResponses.Select(v => v.ToString())));
            }

            Version = first;
        }

        private CodexNode CreateOnlineCodexNode(RunningContainer c, IPluginTools tools, ICodexNodeFactory factory)
        {
            var watcher = factory.CreateCrashWatcher(c);
            var access = new CodexAccess(tools, c, watcher);
            return factory.CreateOnlineCodexNode(access, this);
        }
    }
}
