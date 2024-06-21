using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Utils;

namespace CodexDiscordBotPlugin
{
    public class CodexDiscordBotPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private const string ExpectedStartupMessage = "Debug option is set. Discord connection disabled!";
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

        public RunningPod Deploy(DiscordBotStartupConfig config)
        {
            var workflow = tools.CreateWorkflow();
            return StartContainer(workflow, config);
        }

        public RunningPod DeployRewarder(RewarderBotStartupConfig config)
        {
            var workflow = tools.CreateWorkflow();
            return StartRewarderContainer(workflow, config);
        }

        private RunningPod StartContainer(IStartupWorkflow workflow, DiscordBotStartupConfig config)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = config.Name;
            startupConfig.Add(config);
            var pod = workflow.Start(1, new DiscordBotContainerRecipe(), startupConfig).WaitForOnline();
            WaitForStartupMessage(workflow, pod);
            workflow.CreateCrashWatcher(pod.Containers.Single()).Start();
            return pod;
        }

        private RunningPod StartRewarderContainer(IStartupWorkflow workflow, RewarderBotStartupConfig config)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = config.Name;
            startupConfig.Add(config);
            var pod = workflow.Start(1, new RewarderBotContainerRecipe(), startupConfig).WaitForOnline();
            workflow.CreateCrashWatcher(pod.Containers.Single()).Start();
            return pod;
        }

        private void WaitForStartupMessage(IStartupWorkflow workflow, RunningPod pod)
        {
            var finder = new LogLineFinder(ExpectedStartupMessage, workflow);
            Time.WaitUntil(() =>
            {
                finder.FindLine(pod);
                return finder.Found;
            }, nameof(WaitForStartupMessage));
        }

        public class LogLineFinder : LogHandler
        {
            private readonly string message;
            private readonly IStartupWorkflow workflow;

            public LogLineFinder(string message, IStartupWorkflow workflow)
            {
                this.message = message;
                this.workflow = workflow;
            }

            public void FindLine(RunningPod pod)
            {
                Found = false;
                foreach (var c in pod.Containers)
                {
                    workflow.DownloadContainerLog(c, this);
                    if (Found) return;
                }
            }

            public bool Found { get; private set; }

            protected override void ProcessLine(string line)
            {
                if (!Found && line.Contains(message)) Found = true;
            }
        }
    }
}
