using CodexClient;
using CodexClient.Hooks;
using Core;

namespace CodexPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static ICodexInstance[] DeployCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        {
            return Plugin(ci).DeployCodexNodes(number, setup);
        }

        public static ICodexNodeGroup WrapCodexContainers(this CoreInterface ci, ICodexInstance[] instances)
        {
            return Plugin(ci).WrapCodexContainers(instances);
        }

        public static ICodexNode StartCodexNode(this CoreInterface ci)
        {
            return ci.StartCodexNodes(1)[0];
        }

        public static ICodexNode StartCodexNode(this CoreInterface ci, Action<ICodexSetup> setup)
        {
            return ci.StartCodexNodes(1, setup)[0];
        }

        public static ICodexNodeGroup StartCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        {
            var rc = ci.DeployCodexNodes(number, setup);
            var result = ci.WrapCodexContainers(rc);
            Plugin(ci).WireUpMarketplace(result, setup);
            return result;
        }

        public static ICodexNodeGroup StartCodexNodes(this CoreInterface ci, int number)
        {
            return ci.StartCodexNodes(number, s => { });
        }

        public static void AddCodexHooksProvider(this CoreInterface ci, ICodexHooksProvider hooksProvider)
        {
            Plugin(ci).AddCodexHooksProvider(hooksProvider);
        }

        private static CodexPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexPlugin>();
        }
    }
}
