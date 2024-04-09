namespace KubernetesWorkflow.Types
{
    public class PodInfo
    {
        public PodInfo(string name, string ip, string k8sNodeName)
        {
            Name = name;
            Ip = ip;
            K8SNodeName = k8sNodeName;
        }

        public string Name { get; }
        public string Ip { get; }
        public string K8SNodeName { get; }
    }
}
