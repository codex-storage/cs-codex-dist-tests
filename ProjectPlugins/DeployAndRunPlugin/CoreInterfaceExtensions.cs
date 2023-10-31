using Core;
using KubernetesWorkflow;

namespace DeployAndRunPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningContainer DeployAndRunContinuousTests(this CoreInterface ci, RunConfig runConfig)
        {
            return ci.GetPlugin<DeployAndRunPlugin>().Run(runConfig);
        }
    }
}
