using Core;
using KubernetesWorkflow;

namespace DistTestCore
{
    public class Configuration
    {
        private readonly string? kubeConfigFile;
        private readonly string logPath;
        private readonly string dataFilesPath;

        public Configuration()
        {
            kubeConfigFile = GetNullableEnvVarOrDefault("KUBECONFIG", null);
            logPath = GetEnvVarOrDefault("LOGPATH", "CodexTestLogs");
            dataFilesPath = GetEnvVarOrDefault("DATAFILEPATH", "TestDataFiles");
            AlwaysDownloadContainerLogs = true; // !string.IsNullOrEmpty(GetEnvVarOrDefault("ALWAYS_LOGS", ""));
        }

        public Configuration(string? kubeConfigFile, string logPath, string dataFilesPath)
        {
            this.kubeConfigFile = kubeConfigFile;
            this.logPath = logPath;
            this.dataFilesPath = dataFilesPath;
        }

        /// <summary>
        /// Does not override [DontDownloadLogs] attribute.
        /// </summary>
        public bool AlwaysDownloadContainerLogs { get; set; }

        public KubernetesWorkflow.Configuration GetK8sConfiguration(ITimeSet timeSet, string k8sNamespace, Func<string?, string?> replacer)
        {
            return GetK8sConfiguration(timeSet, new DoNothingK8sHooks(), k8sNamespace, replacer);
        }

        public KubernetesWorkflow.Configuration GetK8sConfiguration(ITimeSet timeSet, IK8sHooks hooks, string k8sNamespace, Func<string?, string?> replacer)
        {
            var config = new KubernetesWorkflow.Configuration(
                kubeConfigFile: kubeConfigFile,
                operationTimeout: timeSet.K8sOperationTimeout(),
                retryDelay: timeSet.K8sOperationRetryDelay(),
                kubernetesNamespace: k8sNamespace
            );

            config.AllowNamespaceOverride = false;
            config.Hooks = hooks;
            config.Replacer = replacer;

            return config;
        }

        public Logging.LogConfig GetLogConfig()
        {
            return new Logging.LogConfig(logPath);
        }

        public string GetFileManagerFolder()
        {
            return dataFilesPath;
        }

        private static string GetEnvVarOrDefault(string varName, string defaultValue)
        {
            var v = Environment.GetEnvironmentVariable(varName);
            if (v == null) return defaultValue;
            return v;
        }

        private static string? GetNullableEnvVarOrDefault(string varName, string? defaultValue)
        {
            var v = Environment.GetEnvironmentVariable(varName);
            if (v == null) return defaultValue;
            return v;
        }
    }
}
