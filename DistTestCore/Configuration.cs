using KubernetesWorkflow;

namespace DistTestCore
{
    public class Configuration
    {
        public KubernetesWorkflow.Configuration GetK8sConfiguration()
        {
            return new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: "ct-",
                kubeConfigFile: null,
                operationTimeout: Timing.K8sOperationTimeout(),
                retryDelay: Timing.K8sServiceDelay(),
                locationMap: new[]
                {
                    new ConfigurationLocationEntry(Location.BensOldGamingMachine, "worker01"),
                    new ConfigurationLocationEntry(Location.BensLaptop, "worker02"),
                } 
            );
        }

        public Logging.LogConfig GetLogConfig()
        {
            return new Logging.LogConfig("CodexTestLogs", debugEnabled: true);
        }

        public string GetFileManagerFolder()
        {
            return "TestDataFiles";
        }
    }
}
