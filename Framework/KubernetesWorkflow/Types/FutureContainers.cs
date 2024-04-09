namespace KubernetesWorkflow.Types
{
    public class FutureContainers
    {
        private readonly RunningContainers runningContainers;
        private readonly StartupWorkflow workflow;

        public FutureContainers(RunningContainers runningContainers, StartupWorkflow workflow)
        {
            this.runningContainers = runningContainers;
            this.workflow = workflow;
        }

        public RunningContainers WaitForOnline()
        {
            workflow.WaitUntilOnline(runningContainers);
            return runningContainers;
        }
    }
}
