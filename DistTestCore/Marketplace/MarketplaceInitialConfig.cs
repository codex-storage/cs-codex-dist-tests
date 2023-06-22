namespace DistTestCore.Marketplace
{
    public class MarketplaceInitialConfig
    {
        public MarketplaceInitialConfig(Ether initialEth, TestToken initialTestTokens, bool isValidator)
        {
            InitialEth = initialEth;
            InitialTestTokens = initialTestTokens;
            IsValidator = isValidator;
        }

        public Ether InitialEth { get; }
        public TestToken InitialTestTokens { get; }
        public bool IsValidator { get; }
        public int? AccountIndexOverride { get; set; }
    }
}
