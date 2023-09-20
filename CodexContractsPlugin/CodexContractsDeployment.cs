namespace CodexContractsPlugin
{
    public interface ICodexContractsDeployment
    {
        string MarketplaceAddress { get; }
        string Abi { get; }
        string TokenAddress { get; }
    }

    public class CodexContractsDeployment : ICodexContractsDeployment
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
