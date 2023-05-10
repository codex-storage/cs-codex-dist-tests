namespace DistTestCore
{
    public class AutoBootstrapDistTest : DistTest
    {
        private IOnlineCodexNode? bootstrapNode;

        public override IOnlineCodexNode SetupCodexBootstrapNode(Action<ICodexSetup> setup)
        {
            throw new Exception("AutoBootstrapDistTest creates and attaches a single boostrap node for you. " +
                "If you want to control the bootstrap node from your test, please use DistTest instead.");
        }

        public override ICodexNodeGroup SetupCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = new CodexSetup(numberOfNodes);
            setup(codexSetup);
            codexSetup.WithBootstrapNode(EnsureBootstapNode());
            return BringOnline(codexSetup);
        }

        protected IOnlineCodexNode BootstrapNode
        {
            get
            {
                return EnsureBootstapNode();
            }
        }

        private IOnlineCodexNode EnsureBootstapNode()
        {
            if (bootstrapNode == null)
            {
                bootstrapNode = base.SetupCodexBootstrapNode(s => { });
            }
            return bootstrapNode;
        }
    }
}
