using CodexClient;
using CodexPlugin;
using DistTestCore;

namespace CodexTests
{
    public class AutoBootstrapComponent : ILifecycleComponent
    {
        public ICodexNode? BootstrapNode { get; private set; } = null;

        public void Start(ILifecycleComponentAccess access)
        {
            if (BootstrapNode != null) return;

            var tl = access.Get<TestLifecycle>();
            var ci = tl.CoreInterface;
            var testNamespace = tl.TestNamespace;

            BootstrapNode = ci.StartCodexNode(s => s.WithName("BOOTSTRAP_" + testNamespace));
        }

        public void ApplyBootstrapNode(ICodexSetup setup)
        {
            if (BootstrapNode == null) return;

            setup.WithBootstrapNode(BootstrapNode);
        }

        public void Stop(ILifecycleComponentAccess access, DistTestResult result)
        {
            if (BootstrapNode == null) return;
            BootstrapNode.Stop(waitTillStopped: false);
        }
    }

    public class AutoBootstrapDistTest : CodexDistTest
    {

        protected override void CreateComponents(ILifecycleComponentCollector collector)
        {
            base.CreateComponents(collector);
            collector.AddComponent(new AutoBootstrapComponent());
        }

        protected override void OnCodexSetup(ICodexSetup setup)
        {
            Get<AutoBootstrapComponent>().ApplyBootstrapNode(setup);
        }

        protected ICodexNode BootstrapNode
        {
            get
            {
                var bn = Get<AutoBootstrapComponent>().BootstrapNode;
                if (bn == null) throw new InvalidOperationException("BootstrapNode accessed before initialized.");
                return bn;
            }
        }
    }
}
