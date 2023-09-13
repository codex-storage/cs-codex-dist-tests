using CodexPlugin;
using DistTestCore;
using DistTestCore.Helpers;
using NUnit.Framework;

namespace Tests
{
    public class AutoBootstrapDistTest : CodexDistTest
    {
        private readonly List<IOnlineCodexNode> onlineCodexNodes = new List<IOnlineCodexNode>();

        [SetUp]
        public void SetUpBootstrapNode()
        {
            BootstrapNode = AddCodex(s => s.WithName("BOOTSTRAP"));
            onlineCodexNodes.Add(BootstrapNode);
        }

        protected override void OnCodexSetup(ICodexSetup setup)
        {
            if (BootstrapNode != null) setup.WithBootstrapNode(BootstrapNode);
        }

        protected IOnlineCodexNode? BootstrapNode { get; private set; }
    }
}
