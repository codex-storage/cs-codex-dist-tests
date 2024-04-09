namespace KubernetesWorkflow.Types
{
    public class K8sNodeLabel
    {
        public K8sNodeLabel(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Value { get; }
    }
}
