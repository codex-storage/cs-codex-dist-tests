using Core;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public class CodexPlugin : IProjectPlugin, IHasLogPrefix
    {
        private readonly CodexStarter codexStarter;
        private readonly IPluginTools tools;
        private readonly CodexLogLevel defaultLogLevel = CodexLogLevel.Trace;

        public CodexPlugin(IPluginTools tools)
        {
            codexStarter = new CodexStarter(tools);
            this.tools = tools;
        }

        public string LogPrefix => "(Codex) ";

        public void Announce()
        {
            tools.GetLog().Log("hello from codex plugin. codex container info here.");
        }

        public void Decommission()
        {
        }

        public RunningContainers[] StartCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = new CodexSetup(numberOfNodes);
            codexSetup.LogLevel = defaultLogLevel;
            setup(codexSetup);
            return codexStarter.BringOnline(codexSetup);
        }

        public ICodexNodeGroup WrapCodexContainers(RunningContainers[] containers)
        {
            return codexStarter.WrapCodexContainers(containers);
        }
    }
}
