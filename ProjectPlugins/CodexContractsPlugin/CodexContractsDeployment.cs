namespace CodexContractsPlugin
{
    public class CodexContractsDeployment
    {
        public CodexContractsDeployment(string marketplaceAddress, string abi, string tokenAddress)
        {
            MarketplaceAddress = marketplaceAddress;
            Abi = abi;
            TokenAddress = tokenAddress;
        }

        public string MarketplaceAddress { get; }
        public string Abi { get; }
        public string TokenAddress { get; }
    }
}
