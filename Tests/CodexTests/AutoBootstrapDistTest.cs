using CodexPlugin;
using NUnit.Framework;

namespace Tests
{
    public class AutoBootstrapDistTest : CodexDistTest
    {
        private readonly List<ICodexNode> onlineCodexNodes = new List<ICodexNode>();

        [SetUp]
        public void SetUpBootstrapNode()
        {
            BootstrapNode = AddCodex(s => s.WithName("BOOTSTRAP"));
            onlineCodexNodes.Add(BootstrapNode);
        }

        [TearDown]
        public void TearDownBootstrapNode()
        {
            BootstrapNode = null;
        }

        protected override void OnCodexSetup(ICodexSetup setup)
        {
            if (BootstrapNode != null) setup.WithBootstrapNode(BootstrapNode);
        }

        protected ICodexNode? BootstrapNode { get; private set; }
    }
}
