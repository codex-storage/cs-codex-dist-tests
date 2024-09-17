using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace BittorrentPlugin
{
    public class BittorrentContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "bittorrent";
        public override string Image => "thatbenbierens/bittorrentdriver:init";

        public static string ApiPortTag = "API_PORT";
        public static string TrackerPortTag = "TRACKER_PORT";
        public static string PeerPortTag = "PEER_PORT";
        public static int TrackerPort = 31010;
        public static int PeerPort = 31012;

        protected override void Initialize(StartupConfig config)
        {
            AddExposedPort(TrackerPort, TrackerPortTag);
            AddExposedPort(PeerPort, PeerPortTag);
            AddExposedPortAndVar("APIPORT", ApiPortTag);
        }
    }
}
