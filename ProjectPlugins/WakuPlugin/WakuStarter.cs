using Core;
using KubernetesWorkflow;

namespace WakuPlugin
{
    public class WakuStarter
    {
        private readonly IPluginTools tools;

        public WakuStarter(IPluginTools tools)
        {
            this.tools = tools;
        }

        public RunningContainers[] Start(int numberOfNodes, Action<IWakuSetup> setup)
        {
            var result = new List<RunningContainers>();
            var workflow = tools.CreateWorkflow();
            var startupConfig = CreateStartupConfig(setup);

            for (var i = 0; i < numberOfNodes; i++)
            {
                result.Add(workflow.Start(1, new WakuPluginContainerRecipe(), startupConfig));
            }

            return result.ToArray();
        }

        public IWakuNode Wrap(RunningContainer container)
        {
            return new WakuNode(tools, container);
        }

        private StartupConfig CreateStartupConfig(Action<IWakuSetup> setup)
        {
            var config = new WakuSetup();
            setup(config);
            var startupConfig = new StartupConfig();
            startupConfig.Add(config);
            startupConfig.NameOverride = config.Name;
            return startupConfig;
        }
    }
}
