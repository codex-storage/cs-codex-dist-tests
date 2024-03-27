using CodexContractsPlugin;
using GethPlugin;

namespace CodexPlugin
{
    public class MarketplaceInitialConfig
    {
        public MarketplaceInitialConfig(MarketplaceSetup marketplaceSetup, IGethNode gethNode, ICodexContracts codexContracts)
        {
            MarketplaceSetup = marketplaceSetup;
            GethNode = gethNode;
            CodexContracts = codexContracts;
        }

        public MarketplaceSetup MarketplaceSetup { get; }
        public IGethNode GethNode { get; }
        public ICodexContracts CodexContracts { get; }
    }
}
