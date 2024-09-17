using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace BittorrentPlugin
{
    public class BittorrentPlugin : IProjectPlugin
    {
        private readonly IPluginTools tools;

        public BittorrentPlugin(IPluginTools tools)
        {
            this.tools = tools;
        }

        public void Announce()
        {
            tools.GetLog().Log("Loaded Bittorrent plugin");
        }

        public void Decommission()
        {
        }

        public IBittorrentNode StartNode()
        {
            var flow = tools.CreateWorkflow();
            var pod = flow.Start(1, new BittorrentContainerRecipe(), new StartupConfig()).WaitForOnline();
            var container = pod.Containers.Single();

            return new BittorrentNode(tools, container);
        }
    }
}
