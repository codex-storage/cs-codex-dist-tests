using Core;
using KubernetesWorkflow.Types;

namespace CodexPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningPod[] DeployCodexNodes(this CoreInterface ci, int number, Action<ICodexSetup> setup)
        {
            return Plugin(ci).DeployCodexNodes(number, setup);
        }

        public static ICodexNodeGroup WrapCodexContainers(this CoreInterface ci, RunningPod[] containers)
        {
            return Plugin(ci).WrapCodexContainers(ci, containers);
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

        private static CodexPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexPlugin>();
        }
    }
}
