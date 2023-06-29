using DistTestCore;
using DistTestCore.Codex;
using KubernetesWorkflow;
using Logging;

namespace ContinuousTests
{
    public class CodexAccessFactory
    {
        public CodexAccess[] Create(RunningContainer[] containers, BaseLog log, ITimeSet timeSet)
        {
            return containers.Select(container =>
            {
                var address = container.ClusterExternalAddress;
                return new CodexAccess(log, container, timeSet, address);
            }).ToArray();
        }
    }
}
