using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
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

            var gethNode = new CustomGethNode(log, GethInput.GethHost, GethInput.GethPort, GethInput.PrivateKey);

            var config = GetCodexMarketplaceConfig(gethNode, GethInput.MarketplaceAddress);

            var contractsDeployment = new CodexContractsDeployment(
                config: config,
                marketplaceAddress: GethInput.MarketplaceAddress,
                abi: GethInput.ABI,
                tokenAddress: GethInput.TokenAddress
            );

            var contracts = new CodexContractsAccess(log, gethNode, contractsDeployment);

            return new GethConnector(gethNode, contracts);
        }

        private static MarketplaceConfig GetCodexMarketplaceConfig(IGethNode gethNode, string marketplaceAddress)
        {
            var func = new ConfigurationFunctionBase();
            var response = gethNode.Call<ConfigurationFunctionBase, ConfigurationOutputDTO>(marketplaceAddress, func);
            return response.ReturnValue1;
        }

        private GethConnector(IGethNode gethNode, ICodexContracts codexContracts)
        {
            GethNode = gethNode;
            CodexContracts = codexContracts;
        }
    }
}
