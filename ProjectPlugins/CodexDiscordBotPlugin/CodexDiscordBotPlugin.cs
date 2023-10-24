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
            metadata.Add("codexdiscordbotid", "todo");
        }

        public void Decommission()
        {
        }

        public RunningContainer Deploy(DiscordBotStartupConfig config)
        {
            var workflow = tools.CreateWorkflow();
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = config.Name;
            startupConfig.Add(config);
            var rc = workflow.Start(1, new DiscordBotContainerRecipe(), startupConfig);

            // write deployment into endpoints folder.


            return rc.Containers.Single();
        }
    }
}
