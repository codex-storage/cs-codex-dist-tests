using Core;
using MetricsPlugin;
using System.Collections;
using Utils;

namespace CodexPlugin
{
    public interface ICodexNodeGroup : IEnumerable<ICodexNode>, IHasManyMetricScrapeTargets
    {
        void Stop(bool waitTillStopped);
        ICodexNode this[int index] { get; }
    }

    public class CodexNodeGroup : ICodexNodeGroup
    {
        private readonly CodexNode[] nodes;

        public CodexNodeGroup(IPluginTools tools, CodexNode[] nodes)
        {
            this.nodes = nodes;
            Version = new DebugInfoVersion();
        }

        public ICodexNode this[int index]
        {
            get
            {
                return Nodes[index];
            }
        }

        public void Stop(bool waitTillStopped)
        {
            foreach (var node in Nodes) node.Stop(waitTillStopped);
        }

        public void Stop(CodexNode node, bool waitTillStopped)
        {
            node.Stop(waitTillStopped);
        }

        public ICodexNode[] Nodes => nodes;
        public DebugInfoVersion Version { get; private set; }
        public Address[] ScrapeTargets => Nodes.Select(n => n.MetricsScrapeTarget).ToArray();

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
            return $"group:[{string.Join(",", Nodes.Select(n => n.GetName()))}]";
        }

        public void EnsureOnline()
        {
            foreach (var node in nodes) node.Initialize();
            var versionResponses = Nodes.Select(n => n.Version);

            var first = versionResponses.First();
            if (!versionResponses.All(v => v.Version == first.Version && v.Revision == first.Revision))
            {
                throw new Exception("Inconsistent version information received from one or more Codex nodes: " +
                    string.Join(",", versionResponses.Select(v => v.ToString())));
            }

            Version = first;
        }
    }
}
