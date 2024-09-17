using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace BittorrentPlugin
{
    public class BittorrentContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "bittorrent";
        public override string Image => "thatbenbierens/bittorrentdriver:init6";

        public static string ApiPortTag = "API_PORT";
        public static string TrackerPortTag = "TRACKER_PORT";
        public static string PeerPortTag = "PEER_PORT";

        protected override void Initialize(StartupConfig config)
        {
            AddInternalPortAndVar("TRACKERPORT", TrackerPortTag);
            AddInternalPortAndVar("PEERPORT", PeerPortTag);
            AddExposedPortAndVar("APIPORT", ApiPortTag);
        }
    }
}
