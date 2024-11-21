using CodexContractsPlugin.Marketplace;

namespace CodexContractsPlugin
{
    public class CodexContractsDeployment
    {
        public CodexContractsDeployment(MarketplaceConfig config, string marketplaceAddress, string abi, string tokenAddress)
        {
            Config = config;
            MarketplaceAddress = marketplaceAddress;
            Abi = abi;
            TokenAddress = tokenAddress;
        }

        public MarketplaceConfig Config { get; }
        public string MarketplaceAddress { get; }
        public string Abi { get; }
        public string TokenAddress { get; }
    }
}
