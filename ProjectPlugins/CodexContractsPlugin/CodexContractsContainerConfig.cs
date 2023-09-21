using GethPlugin;

namespace CodexContractsPlugin
{
    public class CodexContractsContainerConfig
    {
        public CodexContractsContainerConfig(IGethNode gethNode)
        {
            GethNode = gethNode;
        }

        public IGethNode GethNode { get; }
    }
}
