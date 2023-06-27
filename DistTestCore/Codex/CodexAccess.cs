using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexAccess
    {
        private readonly TestLifecycle lifecycle;

        public CodexAccess(TestLifecycle lifecycle, RunningContainer runningContainer)
        {
            this.lifecycle = lifecycle;
            Container = runningContainer;

            var address = lifecycle.Configuration.GetAddress(Container);
            Node = new CodexNode(lifecycle.Log, lifecycle.TimeSet, address);
        }

        public RunningContainer Container { get; }
        public CodexNode Node { get; }

        public void EnsureOnline()
        {
            try
            {
                var debugInfo = Node.GetDebugInfo();
                if (debugInfo == null || string.IsNullOrEmpty(debugInfo.id)) throw new InvalidOperationException("Unable to get debug-info from codex node at startup.");

                var nodePeerId = debugInfo.id;
                var nodeName = Container.Name;
                lifecycle.Log.AddStringReplace(nodePeerId, nodeName);
                lifecycle.Log.AddStringReplace(debugInfo.table.localNode.nodeId, nodeName);
            }
            catch (Exception e)
            {
                lifecycle.Log.Error($"Failed to start codex node: {e}. Test infra failure.");
                throw new InvalidOperationException($"Failed to start codex node. Test infra failure.", e);
            }
        }
    }
}
