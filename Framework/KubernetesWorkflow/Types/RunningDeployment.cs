namespace KubernetesWorkflow.Types
{
    public class RunningDeployment
    {
        public RunningDeployment(string name, string podLabel)
        {
            Name = name;
            PodLabel = podLabel;
        }

        public string Name { get; }
        public string PodLabel { get; }
    }
}
