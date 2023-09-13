using CodexPlugin;
using DistTestCore;
using NUnit.Framework;

namespace Tests
{
    public class AutoBootstrapDistTest : DistTest
    {
        public IOnlineCodexNode AddCodex()
        {
            return AddCodex(s => { });
        }

        public IOnlineCodexNode AddCodex(Action<ICodexSetup> setup)
        {
            return Ci.SetupCodexNode(s =>
            {
                setup(s);
                s.WithBootstrapNode(BootstrapNode);
            });
        }

        public ICodexNodeGroup AddCodex(int numberOfNodes)
        {
            return Ci.SetupCodexNodes(numberOfNodes, s => s.WithBootstrapNode(BootstrapNode));
        }

        public ICodexNodeGroup AddCodex(int numberOfNodes, Action<ICodexSetup> setup)
        {
            return Ci.SetupCodexNodes(numberOfNodes, s =>
            {
                setup(s);
                s.WithBootstrapNode(BootstrapNode);
            });
        }

        [SetUp]
        public void SetUpBootstrapNode()
        {
            BootstrapNode = Ci.SetupCodexNode(s => s.WithName("BOOTSTRAP"));
        }

        protected IOnlineCodexNode BootstrapNode { get; private set; } = null!;
    }
}
