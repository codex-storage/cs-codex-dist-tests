using Core;
using GethPlugin;

namespace CodexContractsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static IMarketplaceInfo DeployCodexContracts(this CoreInterface ci, IGethNodeInfo gethNode)
        {
            return Plugin(ci).DeployContracts(gethNode);
        }

        private static CodexContractsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexContractsPlugin>();
        }
    }
}
