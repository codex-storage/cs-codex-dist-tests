using DistTestCore;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public class Plugin : IProjectPlugin
    {
        private readonly CodexStarter codexStarter;

        public Plugin(IPluginActions actions)
        {
            codexStarter = new CodexStarter(actions);

            DistTestExtensions.Plugin = this;
        }

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

        public IOnlineCodexNode SetupCodexNode(Action<ICodexSetup> setup)
        {
            return null!;
        }

        public ICodexNodeGroup SetupCodexNodes(int number)
        {
            var rc = StartCodexNodes(1, s => { });
            return WrapCodexContainers(rc);
        }
    }
}
