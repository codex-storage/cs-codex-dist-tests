namespace KubernetesWorkflow.Types
{
    public class FutureContainers
    {
        private readonly RunningPod runningPod;
        private readonly StartupWorkflow workflow;

        public FutureContainers(RunningPod runningPod, StartupWorkflow workflow)
        {
            this.runningPod = runningPod;
            this.workflow = workflow;
        }

        public RunningPod WaitForOnline()
        {
            workflow.WaitUntilOnline(runningPod);
            return runningPod;
        }
    }
}
