using KubernetesWorkflow.Recipe;
using KubernetesWorkflow.Types;

namespace KubernetesWorkflow
{
    public interface IK8sHooks
    {
        void OnContainersStarted(RunningContainers runningContainers);
        void OnContainersStopped(RunningContainers runningContainers);
        void OnContainerRecipeCreated(ContainerRecipe recipe);
    }

    public class DoNothingK8sHooks : IK8sHooks
    {
        public void OnContainersStarted(RunningContainers runningContainers)
        {
        }

        public void OnContainersStopped(RunningContainers runningContainers)
        {
        }

        public void OnContainerRecipeCreated(ContainerRecipe recipe)
        {
        }
    }
}
