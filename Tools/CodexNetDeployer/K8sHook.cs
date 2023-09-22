using DistTestCore;
using KubernetesWorkflow;

namespace CodexNetDeployer
{
    public class K8sHook : IK8sHooks
    {
        private readonly string testsTypeLabel;

        public K8sHook(string testsTypeLabel)
        {
            this.testsTypeLabel = testsTypeLabel;
        }

        public void OnContainersStarted(RunningContainers rc)
        {
        }

        public void OnContainersStopped(RunningContainers rc)
        {
        }

        public void OnContainerRecipeCreated(ContainerRecipe recipe)
        {
            recipe.PodLabels.Add("tests-type", testsTypeLabel);
            recipe.PodLabels.Add("runid", NameUtils.GetRunId());
            recipe.PodLabels.Add("testid", NameUtils.GetTestId());
        }
    }
}
