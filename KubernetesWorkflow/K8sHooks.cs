namespace KubernetesWorkflow
{
    public interface IK8sHooks
    {
        void OnContainersStarted(RunningContainers runningContainers);
        void OnContainersStopped(RunningContainers runningContainers);
    }

    public class DoNothingK8sHooks : IK8sHooks
    {
        public void OnContainersStarted(RunningContainers runningContainers)
        {
        }

        public void OnContainersStopped(RunningContainers runningContainers)
        {
        }
    }
}
