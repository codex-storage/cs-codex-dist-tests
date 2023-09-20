using Logging;
using Core;

namespace ContinuousTests
{
    public class EntryPointFactory
    {
        public EntryPoint CreateEntryPoint(string kubeConfigFile, string dataFilePath, string customNamespace, ILog log)
        {
            var kubeConfig = GetKubeConfig(kubeConfigFile);
            var lifecycleConfig = new KubernetesWorkflow.Configuration
            (
                kubeConfigFile: kubeConfig,
                operationTimeout: TimeSpan.FromSeconds(30),
                retryDelay: TimeSpan.FromSeconds(10),
                kubernetesNamespace: customNamespace
            );

            return new EntryPoint(log, lifecycleConfig, dataFilePath);
            //DefaultContainerRecipe.TestsType = "continuous-tests";
        }

        private static string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }
    }
}
