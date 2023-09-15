namespace CodexContractsPlugin
{
    public interface IMarketplaceInfo
    {
    }

    public class MarketplaceInfo : IMarketplaceInfo
    {
        public MarketplaceInfo(string address, string abi, string tokenAddress)
        {
            Address = address;
            Abi = abi;
            TokenAddress = tokenAddress;
        }

        public string Address { get; }
        public string Abi { get; }
        public string TokenAddress { get; }
    }
}
