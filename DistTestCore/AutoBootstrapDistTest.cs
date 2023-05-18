using NUnit.Framework;

namespace DistTestCore
{
    public class AutoBootstrapDistTest : DistTest
    {
        public override IOnlineCodexNode SetupCodexBootstrapNode(Action<ICodexSetup> setup)
        {
            throw new Exception("AutoBootstrapDistTest creates and attaches a single boostrap node for you. " +
                "If you want to control the bootstrap node from your test, please use DistTest instead.");
        }

        public override ICodexNodeGroup SetupCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = new CodexSetup(numberOfNodes);
            setup(codexSetup);
            codexSetup.WithBootstrapNode(BootstrapNode);
            return BringOnline(codexSetup);
        }

        [SetUp]
        public void SetUpBootstrapNode()
        {
            BootstrapNode = BringOnline(new CodexSetup(1)
            {
                LogLevel = Codex.CodexLogLevel.Trace
            })[0];
        }

        protected IOnlineCodexNode BootstrapNode { get; private set; } = null!;
    }
}
