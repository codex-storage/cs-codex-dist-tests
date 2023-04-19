namespace DistTestCore.Marketplace
{
    public class MarketplaceInitialConfig
    {
        public MarketplaceInitialConfig(Ether initialEth, TestToken initialTestTokens)
        {
            InitialEth = initialEth;
            InitialTestTokens = initialTestTokens;
        }

        public Ether InitialEth { get; }
        public TestToken InitialTestTokens { get; }
    }
}
