using GethPlugin;
using Logging;

namespace CodexContractsPlugin
{
    public interface ICodexContracts
    {
        ICodexContractsDeployment Deployment { get; }

        void MintTestTokens(IGethNode gethNode, IHasEthAddress owner, TestToken testTokens);
        void MintTestTokens(IGethNode gethNode, IEthAddress ethAddress, TestToken testTokens);
        TestToken GetTestTokenBalance(IGethNode gethNode, IHasEthAddress owner);
        TestToken GetTestTokenBalance(IGethNode gethNode, IEthAddress ethAddress);
    }

    public class CodexContractsAccess : ICodexContracts
    {
        private readonly ILog log;

        public CodexContractsAccess(ILog log, ICodexContractsDeployment deployment)
        {
            this.log = log;
            Deployment = deployment;
        }

        public ICodexContractsDeployment Deployment { get; }

        public void MintTestTokens(IGethNode gethNode, IHasEthAddress owner, TestToken testTokens)
        {
            MintTestTokens(gethNode, owner.EthAddress, testTokens);
        }

        public void MintTestTokens(IGethNode gethNode, IEthAddress ethAddress, TestToken testTokens)
        {
            var interaction = new ContractInteractions(log, gethNode);
            interaction.MintTestTokens(ethAddress, testTokens.Amount, Deployment.TokenAddress);
        }

        public TestToken GetTestTokenBalance(IGethNode gethNode, IHasEthAddress owner)
        {
            return GetTestTokenBalance(gethNode, owner.EthAddress);
        }

        public TestToken GetTestTokenBalance(IGethNode gethNode, IEthAddress ethAddress)
        {
            var interaction = new ContractInteractions(log, gethNode);
            var balance = interaction.GetBalance(Deployment.TokenAddress, ethAddress.Address);
            return balance.TestTokens();
        }
    }
}
