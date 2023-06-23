using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexDeployment
    {
        public CodexDeployment(GethStartResult gethStartResult, RunningContainer[] codexContainers)
        {
            GethStartResult = gethStartResult;
            CodexContainers = codexContainers;
        }

        public GethStartResult GethStartResult { get; }
        public RunningContainer[] CodexContainers { get; }
    }
}
