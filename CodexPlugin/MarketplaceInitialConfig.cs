using CodexContractsPlugin;
using GethPlugin;

namespace CodexPlugin
{
    public class MarketplaceInitialConfig
    {
        public MarketplaceInitialConfig(IGethNode gethNode, ICodexContracts codexContracts, bool isValidator)
        {
            GethNode = gethNode;
            CodexContracts = codexContracts;
            IsValidator = isValidator;
        }

        public IGethNode GethNode { get; }
        public ICodexContracts CodexContracts { get; }
        public bool IsValidator { get; }

        //public MarketplaceInitialConfig(Ether initialEth, TestToken initialTestTokens, bool isValidator)
        //{
        //    InitialEth = initialEth;
        //    InitialTestTokens = initialTestTokens;
        //    IsValidator = isValidator;
        //}

        //public Ether InitialEth { get; }
        //public TestToken InitialTestTokens { get; }
        //public bool IsValidator { get; }
        //public int? AccountIndexOverride { get; set; }
    }
}
