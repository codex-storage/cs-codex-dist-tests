using GethPlugin;
using Logging;

namespace CodexContractsPlugin
{
    public interface ICodexContracts
    {
        CodexContractsDeployment Deployment { get; }

        void MintTestTokens(IGethNode gethNode, IHasEthAddress owner, TestToken testTokens);
        void MintTestTokens(IGethNode gethNode, EthAddress ethAddress, TestToken testTokens);
        TestToken GetTestTokenBalance(IGethNode gethNode, IHasEthAddress owner);
        TestToken GetTestTokenBalance(IGethNode gethNode, EthAddress ethAddress);
    }

    public class CodexContractsAccess : ICodexContracts
    {
        private readonly ILog log;

        public CodexContractsAccess(ILog log, CodexContractsDeployment deployment)
        {
            this.log = log;
            Deployment = deployment;
        }

        public CodexContractsDeployment Deployment { get; }

        public void MintTestTokens(IGethNode gethNode, IHasEthAddress owner, TestToken testTokens)
        {
            MintTestTokens(gethNode, owner.EthAddress, testTokens);
        }

        public void MintTestTokens(IGethNode gethNode, EthAddress ethAddress, TestToken testTokens)
        {
            var interaction = new ContractInteractions(log, gethNode);
            interaction.MintTestTokens(ethAddress, testTokens.Amount, Deployment.TokenAddress);
        }

        public TestToken GetTestTokenBalance(IGethNode gethNode, IHasEthAddress owner)
        {
            return GetTestTokenBalance(gethNode, owner.EthAddress);
        }

        public TestToken GetTestTokenBalance(IGethNode gethNode, EthAddress ethAddress)
        {
            var interaction = new ContractInteractions(log, gethNode);
            var balance = interaction.GetBalance(Deployment.TokenAddress, ethAddress.Address);
            return balance.TestTokens();
        }
    }
}
