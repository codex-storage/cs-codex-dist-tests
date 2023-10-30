using GethPlugin;
using Logging;

namespace CodexContractsPlugin
{
    public interface ICodexContracts
    {
        CodexContractsDeployment Deployment { get; }

        void MintTestTokens(IHasEthAddress owner, TestToken testTokens);
        void MintTestTokens(EthAddress ethAddress, TestToken testTokens);
        TestToken GetTestTokenBalance(IHasEthAddress owner);
        TestToken GetTestTokenBalance(EthAddress ethAddress);
    }

    public class CodexContractsAccess : ICodexContracts
    {
        private readonly ILog log;
        private readonly IGethNode gethNode;

        public CodexContractsAccess(ILog log, IGethNode gethNode, CodexContractsDeployment deployment)
        {
            this.log = log;
            this.gethNode = gethNode;
            Deployment = deployment;
        }

        public CodexContractsDeployment Deployment { get; }

        public void MintTestTokens(IHasEthAddress owner, TestToken testTokens)
        {
            MintTestTokens(owner.EthAddress, testTokens);
        }

        public void MintTestTokens(EthAddress ethAddress, TestToken testTokens)
        {
            var interaction = new ContractInteractions(log, gethNode);
            interaction.MintTestTokens(ethAddress, testTokens.Amount, Deployment.TokenAddress);
        }

        public TestToken GetTestTokenBalance(IHasEthAddress owner)
        {
            return GetTestTokenBalance(owner.EthAddress);
        }

        public TestToken GetTestTokenBalance(EthAddress ethAddress)
        {
            var interaction = new ContractInteractions(log, gethNode);
            var balance = interaction.GetBalance(Deployment.TokenAddress, ethAddress.Address);
            return balance.TestTokens();
        }
    }
}
