namespace KubernetesWorkflow
{
    public class Configuration
    {
        public Configuration(string k8sNamespacePrefix, string? kubeConfigFile, TimeSpan operationTimeout, TimeSpan retryDelay)
        {
            K8sNamespacePrefix = k8sNamespacePrefix;
            KubeConfigFile = kubeConfigFile;
            OperationTimeout = operationTimeout;
            RetryDelay = retryDelay;
        }

        public string K8sNamespacePrefix { get; }
        public string? KubeConfigFile { get; }
        public TimeSpan OperationTimeout { get; }
        public TimeSpan RetryDelay { get; }
    }
}
