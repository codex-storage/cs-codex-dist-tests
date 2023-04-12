using Utils;

namespace KubernetesWorkflow
{
    public class WorkflowNumberSource
    {
        private readonly NumberSource containerNumberSource = new NumberSource(0);
        private readonly NumberSource servicePortNumberSource;

        public WorkflowNumberSource(int workflowNumber, NumberSource servicePortNumberSource)
        {
            WorkflowNumber = workflowNumber;
            this.servicePortNumberSource = servicePortNumberSource;
        }

        public int WorkflowNumber { get; }

        public int GetContainerNumber()
        {
            return containerNumberSource.GetNextNumber();
        }

        public int GetServicePort()
        {
            return servicePortNumberSource.GetNextNumber();
        }
    }
}
