using DistTestCore;
using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;
using KubernetesWorkflow.Types;

namespace CodexNetDeployer
{
    public class K8sHook : IK8sHooks
    {
        private readonly string testsTypeLabel;
        private readonly string deployId;
        private readonly Dictionary<string, string> metadata;

        public K8sHook(string testsTypeLabel, string deployId, Dictionary<string, string> metadata)
        {
            this.testsTypeLabel = testsTypeLabel;
            this.deployId = deployId;
            this.metadata = metadata;
        }

        public void OnContainersStarted(RunningPod rc)
        {
        }

        public void OnContainersStopped(RunningPod rc)
        {
        }

        public void OnContainerRecipeCreated(ContainerRecipe recipe)
        {
            recipe.PodLabels.Add("tests-type", testsTypeLabel);
            recipe.PodLabels.Add("deployid", deployId);
            recipe.PodLabels.Add("testid", NameUtils.GetTestId());

            foreach (var pair in metadata)
            {
                recipe.PodLabels.Add(pair.Key, pair.Value);
            }
        }
    }
}
