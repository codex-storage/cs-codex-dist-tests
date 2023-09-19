namespace CodexContractsPlugin
{
    public interface ICodexContracts
    {
        string MarketplaceAddress { get; }
    }

    public class CodexContractsAccess : ICodexContracts
    {
        public CodexContractsAccess(string marketplaceAddress, string abi, string tokenAddress)
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
