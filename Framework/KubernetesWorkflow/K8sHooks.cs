using KubernetesWorkflow.Recipe;
using KubernetesWorkflow.Types;

namespace KubernetesWorkflow
{
    public interface IK8sHooks
    {
        void OnContainersStarted(RunningPod runningPod);
        void OnContainersStopped(RunningPod runningPod);
        void OnContainerRecipeCreated(ContainerRecipe recipe);
    }

    public class DoNothingK8sHooks : IK8sHooks
    {
        public void OnContainersStarted(RunningPod runningPod)
        {
        }

        public void OnContainersStopped(RunningPod runningPod)
        {
        }

        public void OnContainerRecipeCreated(ContainerRecipe recipe)
        {
        }
    }
}
