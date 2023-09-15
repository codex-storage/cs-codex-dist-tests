using GethPlugin;
using KubernetesWorkflow;

namespace CodexContractsPlugin
{
    public class CodexContractsContainerConfig
    {
        public CodexContractsContainerConfig(IGethNodeInfo gethNode)
        {
            GethNode = gethNode;
        }

        public IGethNodeInfo GethNode { get; }
    }
}
