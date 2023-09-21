using Utils;

namespace KubernetesWorkflow
{
    public class WorkflowNumberSource
    {
        private readonly NumberSource containerNumberSource;

        public WorkflowNumberSource(int workflowNumber, NumberSource containerNumberSource)
        {
            WorkflowNumber = workflowNumber;
            this.containerNumberSource = containerNumberSource;
        }

        public int WorkflowNumber { get; }

        public int GetContainerNumber()
        {
            return containerNumberSource.GetNextNumber();
        }
    }
}
