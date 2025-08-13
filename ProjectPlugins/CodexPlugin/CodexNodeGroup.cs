using CodexClient;
using Core;
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
        private readonly ICodexNode[] nodes;

        public CodexNodeGroup(IPluginTools tools, ICodexNode[] nodes)
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

        public Address[] GetMetricsScrapeTargets()
        {
            return Nodes.Select(n => n.GetMetricsScrapeTarget()).ToArray();
        }

        public IEnumerator<ICodexNode> GetEnumerator()
        {
            return Nodes.Cast<ICodexNode>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        public string Names()
        {
            return $"[{string.Join(",", Nodes.Select(n => n.GetName()))}]";
        }

        public override string ToString()
        {
            return Names();
        }

        public void EnsureOnline()
        {
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

    public static class CodexNodeGroupExtensions
    {
        public static string Names(this ICodexNode[] nodes)
        {
            return $"[{string.Join(",", nodes.Select(n => n.GetName()))}]";
        }

        public static string Names(this List<ICodexNode> nodes)
        {
            return $"[{string.Join(",", nodes.Select(n => n.GetName()))}]";
        }
    }
}
