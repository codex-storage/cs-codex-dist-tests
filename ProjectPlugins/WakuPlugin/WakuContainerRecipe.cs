using KubernetesWorkflow;
using Utils;

namespace WakuPlugin
{
    public class WakuPluginContainerRecipe : ContainerRecipeFactory
    { 
        public override string AppName => "waku";
        //public override string Image => "statusteam/nim-waku:deploy-wakuv2-test";
        public override string Image => "thatbenbierens/nim-waku:try";

        protected override void Initialize(StartupConfig startupConfig)
        {
            SetResourcesRequest(milliCPUs: 100, memory: 100.MB());
            SetResourceLimits(milliCPUs: 4000, memory: 12.GB());

            AddInternalPortAndVar("WAKUNODE2_TCP_PORT");

            AddEnvVar("WAKUNODE2_RPC_ADDRESS", "0.0.0.0");

            AddEnvVar("WAKUNODE2_REST", "1");
            AddEnvVar("WAKUNODE2_REST_ADDRESS", "0.0.0.0");
            AddExposedPortAndVar("WAKUNODE2_REST_PORT", "restport");

            AddEnvVar("WAKUNODE2_DISCV5_DISCOVERY", "1");
            AddInternalPortAndVar("WAKUNODE2_DISCV5_UDP_PORT");
            //AddEnvVar("WAKUNODE2_DISCV5_BOOTSTRAP_NODE", "________<---- enr here");
            AddEnvVar("WAKUNODE2_DISCV5_ENR_AUTO_UPDATEY", "1");

            AddEnvVar("WAKUNODE2_TOPICS", "test_topics_plz");
        }
    }
}
