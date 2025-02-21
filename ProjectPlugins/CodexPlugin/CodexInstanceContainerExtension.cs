using CodexClient;
using KubernetesWorkflow.Types;
using Utils;

namespace CodexPlugin
{
    public static class CodexInstanceContainerExtension
    {
        public static ICodexInstance CreateFromPod(RunningPod pod)
        {
            var container = pod.Containers.Single();

            return new CodexInstance(
                name: container.Name,
                imageName: container.Recipe.Image,
                startUtc: container.Recipe.RecipeCreatedUtc,
                discoveryEndpoint: SetClusterInternalIpAddress(pod, container.GetInternalAddress(CodexContainerRecipe.DiscoveryPortTag)),
                apiEndpoint: container.GetAddress(CodexContainerRecipe.ApiPortTag),
                listenEndpoint: container.GetInternalAddress(CodexContainerRecipe.ListenPortTag),
                ethAccount: container.Recipe.Additionals.Get<EthAccount>(),
                metricsEndpoint: GetMetricsEndpoint(container)
            );
        }

        private static Address SetClusterInternalIpAddress(RunningPod pod, Address address)
        {
            return new Address(
                logName: address.LogName,
                host: pod.PodInfo.Ip,
                port: address.Port
            );
        }

        private static Address? GetMetricsEndpoint(RunningContainer container)
        {
            try
            {
                return container.GetInternalAddress(CodexContainerRecipe.MetricsPortTag);
            }
            catch
            {
                return null;
            }
        }
    }
}
