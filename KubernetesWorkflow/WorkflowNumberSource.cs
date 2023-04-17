using Utils;

namespace KubernetesWorkflow
{
    public class WorkflowNumberSource
    {
        private readonly NumberSource servicePortNumberSource;
        private readonly NumberSource containerNumberSource;

        public WorkflowNumberSource(int workflowNumber, NumberSource servicePortNumberSource, NumberSource containerNumberSource)
        {
            WorkflowNumber = workflowNumber;
            this.servicePortNumberSource = servicePortNumberSource;
            this.containerNumberSource = containerNumberSource;
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
