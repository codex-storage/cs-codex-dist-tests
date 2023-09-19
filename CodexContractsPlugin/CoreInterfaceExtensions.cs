using Core;
using GethPlugin;

namespace CodexContractsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static ICodexContracts DeployCodexContracts(this CoreInterface ci, IGethNode gethNode)
        {
            return Plugin(ci).DeployContracts(gethNode);
        }

        private static CodexContractsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexContractsPlugin>();
        }
    }
}
