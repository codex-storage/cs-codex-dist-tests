using DistTestCore.Codex;
using KubernetesWorkflow;
using Utils;

namespace DistTestCore
{
    public class Configuration
    {
        private readonly string? kubeConfigFile;
        private readonly string logPath;
        private readonly bool logDebug;
        private readonly string dataFilesPath;
        private readonly CodexLogLevel codexLogLevel;
        private readonly TestRunnerLocation runnerLocation;

        public Configuration()
        {
            kubeConfigFile = GetNullableEnvVarOrDefault("KUBECONFIG", null);
            logPath = GetEnvVarOrDefault("LOGPATH", "CodexTestLogs");
            logDebug = GetEnvVarOrDefault("LOGDEBUG", "false").ToLowerInvariant() == "true";
            dataFilesPath = GetEnvVarOrDefault("DATAFILEPATH", "TestDataFiles");
            codexLogLevel = ParseEnum.Parse<CodexLogLevel>(GetEnvVarOrDefault("LOGLEVEL", nameof(CodexLogLevel.Trace)));
            runnerLocation = ParseEnum.Parse<TestRunnerLocation>(GetEnvVarOrDefault("RUNNERLOCATION", nameof(TestRunnerLocation.ExternalToCluster)));
        }

        public Configuration(string? kubeConfigFile, string logPath, bool logDebug, string dataFilesPath, CodexLogLevel codexLogLevel, TestRunnerLocation runnerLocation)
        {
            this.kubeConfigFile = kubeConfigFile;
            this.logPath = logPath;
            this.logDebug = logDebug;
            this.dataFilesPath = dataFilesPath;
            this.codexLogLevel = codexLogLevel;
            this.runnerLocation = runnerLocation;
        }

        public KubernetesWorkflow.Configuration GetK8sConfiguration(ITimeSet timeSet)
        {
            return new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: "ct-",
                kubeConfigFile: kubeConfigFile,
                operationTimeout: timeSet.K8sOperationTimeout(),
                retryDelay: timeSet.WaitForK8sServiceDelay()
            );
        }

        public Logging.LogConfig GetLogConfig()
        {
            return new Logging.LogConfig(logPath, debugEnabled: logDebug);
        }

        public string GetFileManagerFolder()
        {
            return dataFilesPath;
        }

        public CodexLogLevel GetCodexLogLevel()
        {
            return codexLogLevel;
        }

        public TestRunnerLocation GetTestRunnerLocation()
        {
            return runnerLocation;
        }

        public Address GetAddress(RunningContainer container)
        {
            if (GetTestRunnerLocation() == TestRunnerLocation.InternalToCluster)
            {
                return container.ClusterInternalAddress;
            }
            return container.ClusterExternalAddress;
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

    public enum TestRunnerLocation
    {
        ExternalToCluster,
        InternalToCluster,
    }
}
