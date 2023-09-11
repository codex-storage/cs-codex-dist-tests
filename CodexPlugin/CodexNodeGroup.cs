using DistTestCore;
using KubernetesWorkflow;
using Logging;
using System.Collections;

namespace CodexPlugin
{
    public interface ICodexNodeGroup : IEnumerable<IOnlineCodexNode>
    {
        void BringOffline();
        IOnlineCodexNode this[int index] { get; }
    }

    public class CodexNodeGroup : ICodexNodeGroup
    {
        //private readonly TestLifecycle lifecycle;

        public CodexNodeGroup(/*TestLifecycle lifecycle, CodexSetup setup,*/ILog log, ITimeSet timeSet, RunningContainers[] containers, ICodexNodeFactory codexNodeFactory)
        {
            //this.lifecycle = lifecycle;
            //Setup = setup;
            Containers = containers;
            Nodes = containers.Containers().Select(c => CreateOnlineCodexNode(c, log, timeSet, codexNodeFactory)).ToArray();
            Version = new CodexDebugVersionResponse();
        }

        public IOnlineCodexNode this[int index]
        {
            get
            {
                return Nodes[index];
            }
        }

        public void BringOffline()
        {
            //lifecycle.CodexStarter.BringOffline(this);

            //var result = Setup;
            // Clear everything. Prevent accidental use.
            //Setup = null!;
            Nodes = Array.Empty<OnlineCodexNode>();
            Containers = null!;
        }

        //public CodexSetup Setup { get; private set; }
        public RunningContainers[] Containers { get; private set; }
        public OnlineCodexNode[] Nodes { get; private set; }
        public CodexDebugVersionResponse Version { get; private set; }

        public IEnumerator<IOnlineCodexNode> GetEnumerator()
        {
            return Nodes.Cast<IOnlineCodexNode>().GetEnumerator();
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

        private OnlineCodexNode CreateOnlineCodexNode(RunningContainer c, ILog log, ITimeSet timeSet, ICodexNodeFactory factory)
        {
            var access = new CodexAccess(log, c, timeSet, c.Address);
            return factory.CreateOnlineCodexNode(access, this);
        }
    }
}
