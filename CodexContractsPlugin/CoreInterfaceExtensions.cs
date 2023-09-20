using Core;
using GethPlugin;

namespace CodexContractsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static ICodexContractsDeployment DeployCodexContracts(this CoreInterface ci, IGethNode gethNode)
        {
            return Plugin(ci).DeployContracts(gethNode);
        }

        public static ICodexContracts WrapCodexContractsDeployment(this CoreInterface ci, ICodexContractsDeployment deployment)
        {
            return Plugin(ci).WrapDeploy(deployment);
        }

        public static ICodexContracts StartCodexContracts(this CoreInterface ci, IGethNode gethNode)
        {
            var deployment = DeployCodexContracts(ci, gethNode);
            return WrapCodexContractsDeployment(ci, deployment);
        }

        private static CodexContractsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexContractsPlugin>();
        }
    }
}
