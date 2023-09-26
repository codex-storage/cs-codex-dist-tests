using DistTestCore;
using KubernetesWorkflow;

namespace CodexNetDeployer
{
    public class K8sHook : IK8sHooks
    {
        private readonly string testsTypeLabel;
        private readonly Dictionary<string, string> metadata;

        public K8sHook(string testsTypeLabel, Dictionary<string, string> metadata)
        {
            this.testsTypeLabel = testsTypeLabel;
            this.metadata = metadata;
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

            foreach (var pair in metadata)
            {
                recipe.PodLabels.Add(pair.Key, pair.Value);
            }
        }
    }
}
