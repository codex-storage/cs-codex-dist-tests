using CodexClient;
using Core;
using GethPlugin;

namespace CodexContractsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static CodexContractsDeployment DeployCodexContracts(this CoreInterface ci, IGethNode gethNode, DebugInfoVersion versionInfo)
        {
            return Plugin(ci).DeployContracts(ci, gethNode, versionInfo);
        }

        public static ICodexContracts WrapCodexContractsDeployment(this CoreInterface ci, IGethNode gethNode, CodexContractsDeployment deployment)
        {
            return Plugin(ci).WrapDeploy(gethNode, deployment);
        }

        public static ICodexContracts StartCodexContracts(this CoreInterface ci, IGethNode gethNode, DebugInfoVersion versionInfo)
        {
            var deployment = DeployCodexContracts(ci, gethNode, versionInfo);
            return WrapCodexContractsDeployment(ci, gethNode, deployment);
        }

        private static CodexContractsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexContractsPlugin>();
        }
    }
}
