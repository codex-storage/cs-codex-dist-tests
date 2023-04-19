using DistTestCore.Codex;
using KubernetesWorkflow;
using System.Collections;

namespace DistTestCore
{
    public interface ICodexNodeGroup : IEnumerable<IOnlineCodexNode>
    {
        ICodexSetup BringOffline();
        IOnlineCodexNode this[int index] { get; }
    }

    public class CodexNodeGroup : ICodexNodeGroup
    {
        private readonly TestLifecycle lifecycle;

        public CodexNodeGroup(TestLifecycle lifecycle, CodexSetup setup, RunningContainers containers, ICodexNodeFactory codexNodeFactory)
        {
            this.lifecycle = lifecycle;
            Setup = setup;
            Containers = containers;
            Nodes = containers.Containers.Select(c => CreateOnlineCodexNode(c, codexNodeFactory)).ToArray();
        }

        public IOnlineCodexNode this[int index]
        {
            get
            {
                return Nodes[index];
            }
        }

        public ICodexSetup BringOffline()
        {
            lifecycle.CodexStarter.BringOffline(this);

            var result = Setup;
            // Clear everything. Prevent accidental use.
            Setup = null!;
            Nodes = Array.Empty<OnlineCodexNode>();
            Containers = null!;

            return result;
        }

        public CodexSetup Setup { get; private set; }
        public RunningContainers Containers { get; private set; }
        public OnlineCodexNode[] Nodes { get; private set; }

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
            return $"<CodexNodeGroup@{Containers.Describe()}-{Setup.Describe()}>";
        }

        private OnlineCodexNode CreateOnlineCodexNode(RunningContainer c, ICodexNodeFactory factory)
        {
            var access = new CodexAccess(c);
            EnsureOnline(access);
            return factory.CreateOnlineCodexNode(access, this);
        }

        private void EnsureOnline(CodexAccess access)
        {
            try
            {
                var debugInfo = access.GetDebugInfo();
                if (debugInfo == null || string.IsNullOrEmpty(debugInfo.id)) throw new InvalidOperationException("Unable to get debug-info from codex node at startup.");
            }
            catch (Exception e)
            {
                lifecycle.Log.Error($"Failed to start codex node: {e}. Test infra failure.");
                throw new InvalidOperationException($"Failed to start codex node. Test infra failure.", e);
            }
        }
    }
}
