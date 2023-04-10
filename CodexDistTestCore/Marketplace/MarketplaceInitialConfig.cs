namespace CodexDistTestCore.Marketplace
{
    public class MarketplaceInitialConfig
    {
        public MarketplaceInitialConfig(int initialBalance)
        {
            InitialBalance = initialBalance;
        }

        public int InitialBalance { get; }
    }
}
