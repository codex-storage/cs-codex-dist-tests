using DistTestCore;
using DistTestCore.Codex;
using Utils;

namespace CodexNetDeployer
{
    public class Deployer
    {
        private readonly Configuration config;

        public Deployer(Configuration config)
        {
            this.config = config;
        }

        public void Deploy()
        {
            var log = new NullLog();
            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: config.KubeConfigFile,
                logPath: "null",
                logDebug: false,
                dataFilesPath: "notUsed",
                codexLogLevel: ParseEnum.Parse<CodexLogLevel>(config.CodexLogLevel),
                runnerLocation: config.RunnerLocation
            );

            var timeset = new DefaultTimeSet();
            var kubeConfig = new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: config.KubeNamespace,
                kubeConfigFile: config.KubeConfigFile,
                operationTimeout: timeset.K8sOperationTimeout(),
                retryDelay: timeset.WaitForK8sServiceDelay());

            var lifecycle = new TestLifecycle(log, lifecycleConfig, timeset);
            var workflowCreator = new KubernetesWorkflow.WorkflowCreator(log, kubeConfig);
            var starter = new CodexStarter(lifecycle, workflowCreator);
        }
    }
}
