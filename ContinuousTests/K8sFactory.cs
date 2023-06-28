using DistTestCore.Codex;
using DistTestCore;
using KubernetesWorkflow;
using Logging;

namespace ContinuousTests
{
    public class K8sFactory
    {
        public (WorkflowCreator, TestLifecycle) CreateFacilities(Configuration config, string customNamespace, ITimeSet timeSet, BaseLog log)
        {
            var kubeConfig = GetKubeConfig(config.KubeConfigFile);
            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: kubeConfig,
                logPath: "null",
                logDebug: false,
                dataFilesPath: config.LogPath,
                codexLogLevel: CodexLogLevel.Debug,
                runnerLocation: TestRunnerLocation.ExternalToCluster
            );

            var kubeFlowConfig = new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: customNamespace,
            kubeConfigFile: kubeConfig,
                operationTimeout: timeSet.K8sOperationTimeout(),
            retryDelay: timeSet.WaitForK8sServiceDelay());

            var workflowCreator = new WorkflowCreator(log, kubeFlowConfig, testNamespacePostfix: string.Empty);
            var lifecycle = new TestLifecycle(log, lifecycleConfig, timeSet, workflowCreator);

            return (workflowCreator, lifecycle);
        }

        private static string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }
    }
}
