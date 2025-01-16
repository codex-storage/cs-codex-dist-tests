using CodexClient;
using CodexPlugin;
using DistTestCore;
using NUnit.Framework;

namespace CodexTests
{
    public class AutoBootstrapDistTest : CodexDistTest
    {
        private readonly Dictionary<TestLifecycle, ICodexNode> bootstrapNodes = new Dictionary<TestLifecycle, ICodexNode>();

        [SetUp]
        public void SetUpBootstrapNode()
        {
            var tl = Get();
            if (!bootstrapNodes.ContainsKey(tl))
            {
                bootstrapNodes.Add(tl, StartCodex(s => s.WithName("BOOTSTRAP_" + tl.TestNamespace)));
            }
        }

        [TearDown]
        public void TearDownBootstrapNode()
        {
            bootstrapNodes.Remove(Get());
        }

        protected override void OnCodexSetup(ICodexSetup setup)
        {
            var node = BootstrapNode;
            if (node != null) setup.WithBootstrapNode(node);
        }

        protected ICodexNode? BootstrapNode
        {
            get
            {
                var tl = Get();
                if (bootstrapNodes.TryGetValue(tl, out var node))
                {
                    return node;
                }
                return null;
            }
        }
    }
}
