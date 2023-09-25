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

        public RunningContainers[] Start(int numberOfNodes)
        {
            var result = new List<RunningContainers>();
            var workflow = tools.CreateWorkflow();

            for (var i = 0; i < numberOfNodes; i++)
            {
                result.Add(workflow.Start(1, new WakuPluginContainerRecipe(), new StartupConfig()));
            }

            return result.ToArray();
        }
    }
}
