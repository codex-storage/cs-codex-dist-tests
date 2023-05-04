using KubernetesWorkflow;

namespace DistTestCore
{
    public class Configuration
    {
        public KubernetesWorkflow.Configuration GetK8sConfiguration(ITimeSet timeSet)
        {
            return new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: "ct-",
                kubeConfigFile: null,
                operationTimeout: timeSet.K8sOperationTimeout(),
                retryDelay: timeSet.WaitForK8sServiceDelay(),
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
