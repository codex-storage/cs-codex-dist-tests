using DistTestCore.Codex;
using DistTestCore;
using Logging;

namespace ContinuousTests
{
    public class K8sFactory
    {
        public TestLifecycle CreateTestLifecycle(string kubeConfigFile, string logPath, string dataFilePath, string customNamespace, ITimeSet timeSet, BaseLog log)
        {
            var kubeConfig = GetKubeConfig(kubeConfigFile);
            var lifecycleConfig = new DistTestCore.Configuration
            (
                kubeConfigFile: kubeConfig,
                logPath: logPath,
                logDebug: false,
                dataFilesPath: dataFilePath,
                codexLogLevel: CodexLogLevel.Debug,
                k8sNamespacePrefix: customNamespace
            );

            return new TestLifecycle(log, lifecycleConfig, timeSet, "continuous-tests", string.Empty);
        }

        private static string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }
    }
}
