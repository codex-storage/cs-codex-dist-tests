﻿namespace KubernetesWorkflow
{
    public class Configuration
    {
        public Configuration(string? kubeConfigFile, TimeSpan operationTimeout, TimeSpan retryDelay, string kubernetesNamespace)
        {
            KubeConfigFile = kubeConfigFile;
            OperationTimeout = operationTimeout;
            RetryDelay = retryDelay;
            KubernetesNamespace = kubernetesNamespace;
        }

        public string? KubeConfigFile { get; }
        public TimeSpan OperationTimeout { get; }
        public TimeSpan RetryDelay { get; }
        public string KubernetesNamespace { get; }
    }
}
