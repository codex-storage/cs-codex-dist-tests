using Core;
using KubernetesWorkflow;
using Newtonsoft.Json;

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
            var container = StartContainer(workflow, config);
            WriteCodexDeploymentToContainerFile(workflow, container, config);
            return container;
        }

        private RunningContainer StartContainer(IStartupWorkflow workflow, DiscordBotStartupConfig config)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = config.Name;
            startupConfig.Add(config);
            var rc = workflow.Start(1, new DiscordBotContainerRecipe(), startupConfig);
            return rc.Containers.Single();
        }

        private void WriteCodexDeploymentToContainerFile(IStartupWorkflow workflow, RunningContainer rc, DiscordBotStartupConfig config)
        {
            var lines = JsonConvert.SerializeObject(config.CodexDeployment, Formatting.Indented).Split('\n');
            if (lines.Length < 10) throw new Exception("Didn't expect that.");

            var targetFile = DiscordBotContainerRecipe.EndpointsPath;
            var op = ">";

            foreach (var line in lines)
            {
                workflow.ExecuteCommand(rc, $"echo \"{line}\" {op} {targetFile}");
                op = ">>";
            }
        }
    }
}
