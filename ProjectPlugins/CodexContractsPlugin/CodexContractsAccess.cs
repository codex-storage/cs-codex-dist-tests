using GethPlugin;
using Logging;

namespace CodexContractsPlugin
{
    public interface ICodexContracts
    {
        CodexContractsDeployment Deployment { get; }

        bool IsDeployed();
        string MintTestTokens(IHasEthAddress owner, TestToken testTokens);
        string MintTestTokens(EthAddress ethAddress, TestToken testTokens);
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

        public bool IsDeployed()
        {
            var interaction = new ContractInteractions(log, gethNode);
            return !string.IsNullOrEmpty(interaction.GetTokenName(Deployment.TokenAddress));
        }

        public string MintTestTokens(IHasEthAddress owner, TestToken testTokens)
        {
            return MintTestTokens(owner.EthAddress, testTokens);
        }

        public string MintTestTokens(EthAddress ethAddress, TestToken testTokens)
        {
            var interaction = new ContractInteractions(log, gethNode);
            return interaction.MintTestTokens(ethAddress, testTokens.Amount, Deployment.TokenAddress);
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
