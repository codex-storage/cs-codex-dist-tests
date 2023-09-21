using CodexContractsPlugin;
using GethPlugin;

namespace CodexPlugin
{
    public class MarketplaceInitialConfig
    {
        public MarketplaceInitialConfig(IGethNode gethNode, ICodexContracts codexContracts, Ether initialEth, TestToken initialTokens, bool isValidator)
        {
            GethNode = gethNode;
            CodexContracts = codexContracts;
            InitialEth = initialEth;
            InitialTokens = initialTokens;
            IsValidator = isValidator;
        }

        public IGethNode GethNode { get; }
        public ICodexContracts CodexContracts { get; }
        public Ether InitialEth { get; }
        public TestToken InitialTokens { get; }
        public bool IsValidator { get; }
    }
}
