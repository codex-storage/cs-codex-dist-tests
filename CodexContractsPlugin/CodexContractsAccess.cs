using GethPlugin;
using Logging;

namespace CodexContractsPlugin
{
    public interface ICodexContracts
    {
        string MarketplaceAddress { get; }

        void MintTestTokens(IGethNode gethNode, IHasEthAddress owner, TestToken testTokens);
        void MintTestTokens(IGethNode gethNode, IEthAddress ethAddress, TestToken testTokens);
        TestToken GetTestTokenBalance(IGethNode gethNode, IHasEthAddress owner);
        TestToken GetTestTokenBalance(IGethNode gethNode, IEthAddress ethAddress);
    }

    public class CodexContractsAccess : ICodexContracts
    {
        private readonly ILog log;

        public CodexContractsAccess(ILog log, string marketplaceAddress, string abi, string tokenAddress)
        {
            this.log = log;
            MarketplaceAddress = marketplaceAddress;
            Abi = abi;
            TokenAddress = tokenAddress;
        }

        public string MarketplaceAddress { get; }
        public string Abi { get; }
        public string TokenAddress { get; }

        public void MintTestTokens(IGethNode gethNode, IHasEthAddress owner, TestToken testTokens)
        {
            MintTestTokens(gethNode, owner.EthAddress, testTokens);
        }

        public void MintTestTokens(IGethNode gethNode, IEthAddress ethAddress, TestToken testTokens)
        {
            var interaction = new ContractInteractions(log, gethNode);
            interaction.MintTestTokens(ethAddress, testTokens.Amount, TokenAddress);
        }

        public TestToken GetTestTokenBalance(IGethNode gethNode, IHasEthAddress owner)
        {
            return GetTestTokenBalance(gethNode, owner.EthAddress);
        }

        public TestToken GetTestTokenBalance(IGethNode gethNode, IEthAddress ethAddress)
        {
            var interaction = new ContractInteractions(log, gethNode);
            var balance = interaction.GetBalance(TokenAddress, ethAddress.Address);
            return balance.TestTokens();
        }
    }
}
