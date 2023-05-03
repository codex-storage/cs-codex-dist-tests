namespace KubernetesWorkflow
{
    public class Configuration
    {
        public Configuration(string k8sNamespacePrefix, string? kubeConfigFile, TimeSpan operationTimeout, TimeSpan retryDelay, ConfigurationLocationEntry[] locationMap)
        {
            K8sNamespacePrefix = k8sNamespacePrefix;
            KubeConfigFile = kubeConfigFile;
            OperationTimeout = operationTimeout;
            RetryDelay = retryDelay;
            LocationMap = locationMap;
        }

        public string K8sNamespacePrefix { get; }
        public string? KubeConfigFile { get; }
        public TimeSpan OperationTimeout { get; }
        public TimeSpan RetryDelay { get; }
        public ConfigurationLocationEntry[] LocationMap { get; }
    }

    public class ConfigurationLocationEntry
    {
        public ConfigurationLocationEntry(Location location, string workerName)
        {
            Location = location;
            WorkerName = workerName;
        }

        public Location Location { get; }
        public string WorkerName { get; }
    }
}
