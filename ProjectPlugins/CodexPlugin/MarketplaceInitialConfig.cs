using CodexContractsPlugin;
using GethPlugin;

namespace CodexPlugin
{
    public class MarketplaceInitialConfig
    {
        public MarketplaceInitialConfig(MarketplaceSetup marketplaceSetup, IGethNode gethNode, ICodexContracts codexContracts, Ether initialEth, TestToken initialTokens)
        {
            MarketplaceSetup = marketplaceSetup;
            GethNode = gethNode;
            CodexContracts = codexContracts;
            InitialEth = initialEth;
            InitialTokens = initialTokens;
        }

        public MarketplaceSetup MarketplaceSetup { get; }
        public IGethNode GethNode { get; }
        public ICodexContracts CodexContracts { get; }
        public Ether InitialEth { get; }
        public TestToken InitialTokens { get; }
    }
}
