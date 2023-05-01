using KubernetesWorkflow;

namespace DistTestCore
{
    public class Configuration
    {
        public KubernetesWorkflow.Configuration GetK8sConfiguration()
        {
            return new KubernetesWorkflow.Configuration(
                k8sNamespace: "codex-test-ns",
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
            return new Logging.LogConfig("CodexTestLogs", debugEnabled: false);
        }

        public string GetFileManagerFolder()
        {
            return "TestDataFiles";
        }
    }
}
