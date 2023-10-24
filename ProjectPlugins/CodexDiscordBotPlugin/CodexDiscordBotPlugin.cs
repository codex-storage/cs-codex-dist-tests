using Core;
using KubernetesWorkflow;

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

        public RunningContainer Deploy(DiscordBotStartupConfig config)
        {
            var workflow = tools.CreateWorkflow();
            return StartContainer(workflow, config);
        }

        private RunningContainer StartContainer(IStartupWorkflow workflow, DiscordBotStartupConfig config)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = config.Name;
            startupConfig.Add(config);
            var rc = workflow.Start(1, new DiscordBotContainerRecipe(), startupConfig);
            return rc.Containers.Single();
        }
    }
}
