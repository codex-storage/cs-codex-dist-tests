using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;

namespace CodexDiscordBotPlugin
{
    public class CodexDiscordBotPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;

        public CodexDiscordBotPlugin(IPluginTools tools)
        {
            this.tools = tools;
        }

        public string LogPrefix => "(DiscordBot) ";

        public void Announce()
        {
            tools.GetLog().Log($"Codex DiscordBot (BiblioTech) loaded.");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("codexdiscordbotid", new DiscordBotContainerRecipe().Image);
        }

        public void Decommission()
        {
        }

        public RunningContainers Deploy(DiscordBotStartupConfig config)
        {
            var workflow = tools.CreateWorkflow();
            return StartContainer(workflow, config);
        }

        private RunningContainers StartContainer(IStartupWorkflow workflow, DiscordBotStartupConfig config)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = config.Name;
            startupConfig.Add(config);
            return workflow.Start(1, new DiscordBotContainerRecipe(), startupConfig);
        }
    }
}
