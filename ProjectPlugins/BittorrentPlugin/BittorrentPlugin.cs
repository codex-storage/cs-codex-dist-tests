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

        public void Run()
        {
            var flow = tools.CreateWorkflow();
            var trackerPod = flow.Start(1, new TrackerContainerRecipe(), new StartupConfig()).WaitForOnline();
            var trackerContainer = trackerPod.Containers.Single();

            //var msg = flow.ExecuteCommand(trackerContainer, "apt-get", "update");
            //msg = flow.ExecuteCommand(trackerContainer, "apt-get", "install", "npm", "-y");
            //var msg = flow.ExecuteCommand(trackerContainer, "npm", "install", "-g", "bittorrent-tracker");
            //msg = flow.ExecuteCommand(trackerContainer, "bittorrent-tracker", "--port", "30800", "&");

            var clientPod = flow.Start(1, new BittorrentContainerRecipe(), new StartupConfig()).WaitForOnline();
            var clientContainer = clientPod.Containers.Single();

            var msg = flow.ExecuteCommand(clientContainer, "echo", "1234567890987654321",
                ">", "/root/datafile.txt");

            var trackerAddress = trackerContainer.GetAddress(tools.GetLog(), TrackerContainerRecipe.HttpPort);
            if (trackerAddress == null) throw new Exception();
            var trackerAddressStr = trackerAddress.ToString();

            msg = flow.ExecuteCommand(clientContainer, "transmission-create",
                "-o", "/root/outfile.torrent",
                "-t", trackerAddressStr,
                "/root/datafile.txt");

            msg = flow.ExecuteCommand(clientContainer, "cat", "/root/outfile.torrent");

            var a = 0;
        }
    }

    public class TrackerContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "bittorrenttracker";
        public override string Image => "thatbenbierens/bittorrent-tracker:init";

        public static string HttpPort = "http";

        protected override void Initialize(StartupConfig config)
        {
            AddExposedPort(30800, HttpPort);
        }
    }

    public class BittorrentContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "bittorrentclient";
        public override string Image => "thatbenbierens/bittorrent-client:init";

        protected override void Initialize(StartupConfig config)
        {
        }
    }
}
