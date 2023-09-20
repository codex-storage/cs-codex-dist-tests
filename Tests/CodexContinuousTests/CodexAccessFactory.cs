using DistTestCore;
using DistTestCore.Codex;
using KubernetesWorkflow;
using Logging;

namespace ContinuousTests
{
    public class CodexAccessFactory
    {
        public CodexAccess[] Create(Configuration config, RunningContainer[] containers, BaseLog log, ITimeSet timeSet)
        {
            return containers.Select(container =>
            {
                var address = container.ClusterExternalAddress;
                if (config.RunnerLocation == RunnerLocation.InternalToCluster) address = container.ClusterInternalAddress;
                return new CodexAccess(log, container, timeSet, address);
            }).ToArray();
        }
    }
}
