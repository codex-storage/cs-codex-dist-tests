using DistTestCore;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public static class DistTestExtensions
    {
        public static RunningContainers StartCodexNodes(this DistTest distTest, int number, Action<ICodexSetup> setup)
        {
            return null!;
        }

        public static ICodexNodeGroup WrapCodexContainers(this DistTest distTest, RunningContainers containers)
        {
            return null!;
        }

        public static IOnlineCodexNode SetupCodexNode(this DistTest distTest, Action<ICodexSetup> setup)
        {
            return null!;
        }

        public static ICodexNodeGroup SetupCodexNodes(this DistTest distTest, int number)
        {
            return null!;
        }
    }
}
