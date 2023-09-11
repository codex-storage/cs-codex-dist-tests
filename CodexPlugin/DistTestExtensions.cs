using DistTestCore;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public static class DistTestExtensions
    {
        public static Plugin Plugin { get; internal set; } = null!;

        public static RunningContainers[] StartCodexNodes(this DistTest distTest, int number, Action<ICodexSetup> setup)
        {
            return Plugin.StartCodexNodes(number, setup);
        }

        public static ICodexNodeGroup WrapCodexContainers(this DistTest distTest, RunningContainers containers)
        {
            return Plugin.WrapCodexContainers(containers);
        }

        public static IOnlineCodexNode SetupCodexNode(this DistTest distTest, Action<ICodexSetup> setup)
        {
            return Plugin.SetupCodexNode(setup);
        }

        public static ICodexNodeGroup SetupCodexNodes(this DistTest distTest, int number)
        {
            return Plugin.SetupCodexNodes(number);
        }
    }
}
