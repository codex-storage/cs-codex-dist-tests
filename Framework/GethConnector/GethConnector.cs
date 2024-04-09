using CodexContractsPlugin;
using GethPlugin;
using Logging;

namespace GethConnector
{
    public class GethConnector
    {
        public IGethNode GethNode { get; }
        public ICodexContracts CodexContracts { get; }

        public static GethConnector? Initialize(ILog log)
        {
            if (!string.IsNullOrEmpty(GethInput.LoadError))
            {
                var msg = "Geth input incorrect: " + GethInput.LoadError;
                log.Error(msg);
                return null;
            }

            var contractsDeployment = new CodexContractsDeployment(
                marketplaceAddress: GethInput.MarketplaceAddress,
                abi: GethInput.ABI,
                tokenAddress: GethInput.TokenAddress
            );

            var gethNode = new CustomGethNode(log, GethInput.GethHost, GethInput.GethPort, GethInput.PrivateKey);
            var contracts = new CodexContractsAccess(log, gethNode, contractsDeployment);

            return new GethConnector(gethNode, contracts);
        }

        private GethConnector(IGethNode gethNode, ICodexContracts codexContracts)
        {
            GethNode = gethNode;
            CodexContracts = codexContracts;
        }
    }
}
