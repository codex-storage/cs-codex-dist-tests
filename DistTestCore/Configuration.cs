using DistTestCore.Codex;
using KubernetesWorkflow;
using System.Net.NetworkInformation;
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
        private readonly string k8sNamespacePrefix;
        private static RunnerLocation? runnerLocation = null;

        public Configuration()
        {
            kubeConfigFile = GetNullableEnvVarOrDefault("KUBECONFIG", null);
            logPath = GetEnvVarOrDefault("LOGPATH", "CodexTestLogs");
            logDebug = GetEnvVarOrDefault("LOGDEBUG", "false").ToLowerInvariant() == "true";
            dataFilesPath = GetEnvVarOrDefault("DATAFILEPATH", "TestDataFiles");
            codexLogLevel = ParseEnum.Parse<CodexLogLevel>(GetEnvVarOrDefault("LOGLEVEL", nameof(CodexLogLevel.Trace)));
            k8sNamespacePrefix = "ct-";
        }

        public Configuration(string? kubeConfigFile, string logPath, bool logDebug, string dataFilesPath, CodexLogLevel codexLogLevel, string k8sNamespacePrefix)
        {
            this.kubeConfigFile = kubeConfigFile;
            this.logPath = logPath;
            this.logDebug = logDebug;
            this.dataFilesPath = dataFilesPath;
            this.codexLogLevel = codexLogLevel;
            this.k8sNamespacePrefix = k8sNamespacePrefix;
        }

        public KubernetesWorkflow.Configuration GetK8sConfiguration(ITimeSet timeSet)
        {
            return new KubernetesWorkflow.Configuration(
                k8sNamespacePrefix: k8sNamespacePrefix,
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

        public Address GetAddress(RunningContainer container)
        {
            if (runnerLocation == null)
            {
                runnerLocation = RunnerLocationUtils.DetermineRunnerLocation(container);
            }
            
            if (runnerLocation == RunnerLocation.InternalToCluster)
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

    public enum RunnerLocation
    {
        ExternalToCluster,
        InternalToCluster,
    }

    public static class RunnerLocationUtils
    {
        private static bool alreadyDidThat = false;

        public static RunnerLocation DetermineRunnerLocation(RunningContainer container)
        {
            // We want to be sure we don't ping more often than strictly necessary.
            // If we have already determined the location during this application
            // lifetime, don't do it again.
            if (alreadyDidThat) throw new Exception("We already did that.");
            alreadyDidThat = true;

            if (PingHost(container.Pod.PodInfo.Ip))
            {
                return RunnerLocation.InternalToCluster;
            }
            if (PingHost(Format(container.ClusterExternalAddress)))
            {
                return RunnerLocation.ExternalToCluster;
            }

            throw new Exception("Unable to determine runner location.");
        }

        private static string Format(Address host)
        {
            return host.Host
                .Replace("http://", "")
                .Replace("https://", "");
        }

        private static bool PingHost(string host)
        {
            try
            {
                using var pinger = new Ping();
                PingReply reply = pinger.Send(host);
                return reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
            }

            return false;
        }
    }
}
