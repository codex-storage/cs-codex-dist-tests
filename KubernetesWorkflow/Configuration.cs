namespace KubernetesWorkflow
{
    public class Configuration
    {
        public Configuration(string? kubeConfigFile, TimeSpan operationTimeout, TimeSpan retryDelay)
        {
            KubeConfigFile = kubeConfigFile;
            OperationTimeout = operationTimeout;
            RetryDelay = retryDelay;
        }

        public string? KubeConfigFile { get; }
        public TimeSpan OperationTimeout { get; }
        public TimeSpan RetryDelay { get; }
    }
}
