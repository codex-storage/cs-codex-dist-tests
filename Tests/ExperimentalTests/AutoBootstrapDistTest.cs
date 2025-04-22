using CodexClient;
using CodexPlugin;
using DistTestCore;

namespace CodexTests
{
    public class AutoBootstrapDistTest : CodexDistTest
    {
        private readonly Dictionary<TestLifecycle, ICodexNode> bootstrapNodes = new Dictionary<TestLifecycle, ICodexNode>();
        private bool isBooting = false;

        protected override void LifecycleStart(TestLifecycle tl)
        {
            base.LifecycleStart(tl);
            if (!bootstrapNodes.ContainsKey(tl))
            {
                isBooting = true;
                bootstrapNodes.Add(tl, StartCodex(s => s.WithName("BOOTSTRAP_" + tl.TestNamespace)));
                isBooting = false;
            }
        }

        protected override void LifecycleStop(TestLifecycle lifecycle, DistTestResult result)
        {
            bootstrapNodes.Remove(lifecycle);
            base.LifecycleStop(lifecycle, result);
        }

        protected override void OnCodexSetup(ICodexSetup setup)
        {
            if (isBooting) return;

            var node = BootstrapNode;
            if (node != null) setup.WithBootstrapNode(node);
        }

        protected ICodexNode BootstrapNode
        {
            get
            {
                var tl = Get();
                if (bootstrapNodes.TryGetValue(tl, out var node))
                {
                    return node;
                }
                throw new InvalidOperationException("Bootstrap node not yet started.");
            }
        }
    }
}
