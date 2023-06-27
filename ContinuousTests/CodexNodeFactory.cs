using DistTestCore;
using DistTestCore.Codex;
using KubernetesWorkflow;
using Logging;

namespace ContinuousTests
{
    public class CodexNodeFactory
    {
        public CodexNode[] Create(RunningContainer[] containers, BaseLog log, ITimeSet timeSet)
        {
            return containers.Select(container =>
            {
                var address = container.ClusterExternalAddress;
                return new CodexNode(log, timeSet, address);
            }).ToArray();
        }
    }
}
