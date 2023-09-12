using Core;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningContainers[] StartCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        {
            return Plugin(ci).StartCodexNodes(number, setup);
        }

        public static ICodexNodeGroup WrapCodexContainers(this CoreInterface ci, RunningContainers[] containers)
        {
            return Plugin(ci).WrapCodexContainers(containers);
        }

        public static IOnlineCodexNode SetupCodexNode(this CoreInterface ci)
        {
            return Plugin(ci).SetupCodexNode(s => { }); // do more unification here. Keep plugin simpler.
        }

        public static IOnlineCodexNode SetupCodexNode(this CoreInterface ci, Action<ICodexSetup> setup)
        {
            return Plugin(ci).SetupCodexNode(setup);
        }

        public static ICodexNodeGroup SetupCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        {
            return Plugin(ci).SetupCodexNodes(number, setup);
        }

        public static ICodexNodeGroup SetupCodexNodes(this CoreInterface ci, int number)
        {
            return Plugin(ci).SetupCodexNodes(number);
        }

        private static CodexPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexPlugin>();
        }
    }
}
