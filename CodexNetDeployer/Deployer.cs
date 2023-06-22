using DistTestCore;

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
            Log("Initializing...");
            var starter = CreateStarter();

            Log("Preparing configuration...");
            var setup = new CodexSetup(config.NumberOfCodexNodes!.Value, config.CodexLogLevel);

            Log("Creating resources...");
            var group = (CodexNodeGroup) starter.BringOnline(setup);

            var containers = group.Containers;
            foreach (var container in containers.Containers)
            {
                var pod = container.Pod.PodInfo;
                Log($"Container '{container.Name}' online. Pod: '{pod.Name}@{pod.Ip}' on '{pod.K8SNodeName}'.");
            }
        }

        private CodexStarter CreateStarter()
        {
            var log = new NullLog();
            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: config.KubeConfigFile,
                logPath: "null",
                logDebug: false,
                dataFilesPath: "notUsed",
                codexLogLevel: config.CodexLogLevel,
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
            return new CodexStarter(lifecycle, workflowCreator);
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
