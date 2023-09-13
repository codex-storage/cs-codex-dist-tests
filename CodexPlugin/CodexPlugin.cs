using Core;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public class CodexPlugin : IProjectPlugin
    {
        private readonly CodexStarter codexStarter;
        private readonly IPluginTools tools;

        public CodexPlugin(IPluginTools tools)
        {
            codexStarter = new CodexStarter(tools);
            this.tools = tools;
        }

        #region IProjectPlugin Implementation

        public void Announce()
        {
            tools.GetLog().Log("hello from codex plugin. codex container info here.");
        }

        public void Decommission()
        {
        }

        #endregion

        public RunningContainers[] StartCodexNodes(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var codexSetup = new CodexSetup(numberOfNodes, CodexLogLevel.Trace);
            setup(codexSetup);
            return codexStarter.BringOnline(codexSetup);
        }

        public ICodexNodeGroup WrapCodexContainers(RunningContainers[] containers)
        {
            return codexStarter.WrapCodexContainers(containers);
        }
    }
}
