using DistTestCore.Codex;

namespace ContinuousTests.Tests
{
    public class MarketplaceTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 1;

        public override void Run()
        {
        }

        [TestMoment(t: Zero)]
        public void NodePostsStorageRequest()
        {
            //var c = new KubernetesWorkflow.WorkflowCreator(Log, new KubernetesWorkflow.Configuration());
            //var flow = c.CreateWorkflow();
            //var rc = flow.Start(10, KubernetesWorkflow.Location.Unspecified, new CodexContainerRecipe(), new KubernetesWorkflow.StartupConfig());
        }

        [TestMoment(t: DayThree)]
        public void NodeDownloadsStorageRequestData()
        {

        }
    }

}
