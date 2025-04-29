using CodexClient;
using CodexPlugin;
using NUnit.Framework;

namespace CodexTests
{
    public class AutoBootstrapDistTest : CodexDistTest
    {
        private bool isBooting = false;

        public ICodexNode BootstrapNode { get; private set; } = null!;

        [SetUp]
        public void SetupBootstrapNode()
        {
            isBooting = true;
            BootstrapNode = StartCodex(s => s.WithName("BOOTSTRAP_" + GetTestNamespace()));
            isBooting = false;
        }

        [TearDown]
        public void TearDownBootstrapNode()
        {
            BootstrapNode.Stop(waitTillStopped: false);
        }

        protected override void OnCodexSetup(ICodexSetup setup)
        {
            if (isBooting) return;

            var node = BootstrapNode;
            if (node != null) setup.WithBootstrapNode(node);
        }
    }
}
