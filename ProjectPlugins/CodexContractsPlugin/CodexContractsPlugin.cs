using Core;
using GethPlugin;

namespace CodexContractsPlugin
{
    public class CodexContractsPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;
        private readonly CodexContractsStarter starter;
        private readonly VersionRegistry versionRegistry;
        private readonly CodexContractsContainerRecipe recipe;

        public CodexContractsPlugin(IPluginTools tools)
        {
            this.tools = tools;
            versionRegistry = new VersionRegistry(tools.GetLog());
            recipe = new CodexContractsContainerRecipe(versionRegistry);
            starter = new CodexContractsStarter(tools, recipe);
        }

        public string LogPrefix => "(CodexContracts) ";

        public void Awake(IPluginAccess access)
        {
        }

        public void Announce()
        {
            tools.GetLog().Log($"Loaded Codex-Marketplace SmartContracts");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("codexcontractsid", recipe.Image);
        }

        public void Decommission()
        {
        }

        public CodexContractsDeployment DeployContracts(CoreInterface ci, IGethNode gethNode)
        {
            return starter.Deploy(ci, gethNode);
        }

        public ICodexContracts WrapDeploy(IGethNode gethNode, CodexContractsDeployment deployment)
        {
            deployment = SerializeGate.Gate(deployment);
            return starter.Wrap(gethNode, deployment);
        }

        public void SetCodexDockerImageProvider(ICodexDockerImageProvider provider)
        {
            versionRegistry.SetProvider(provider);
        }
    }
}
