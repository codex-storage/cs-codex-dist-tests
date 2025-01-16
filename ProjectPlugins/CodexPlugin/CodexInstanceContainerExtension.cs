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
                discoveryEndpoint: container.GetInternalAddress(CodexContainerRecipe.DiscoveryPortTag),
                apiEndpoint: container.GetAddress(CodexContainerRecipe.ApiPortTag),
                listenEndpoint: container.GetInternalAddress(CodexContainerRecipe.ListenPortTag),
                ethAccount: container.Recipe.Additionals.Get<EthAccount>(),
                metricsEndpoint: GetMetricsEndpoint(container)
            );
        }

        // todo: is this needed for the discovery address??
        //var info = codexAccess.GetPodInfo();
        //return new Address(
        //    logName: $"{GetName()}:DiscoveryPort",
        //    host: info.Ip,
        //    port: Container.Recipe.GetPortByTag(CodexContainerRecipe.DiscoveryPortTag)!.Number
        //);

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
