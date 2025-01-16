using Core;
using GethPlugin;
using KubernetesWorkflow.Types;
using Logging;
using Utils;

namespace CodexPlugin
{
    public interface ICodexInstance
    {
        string Name { get; }
        string ImageName { get; }
        DateTime StartUtc { get; }
        Address DiscoveryEndpoint { get; }
        Address ApiEndpoint { get; }
        Address ListenEndpoint { get; }
        EthAccount? EthAccount { get; }
        Address? MetricsEndpoint { get; }
    }

    public class CodexInstance : ICodexInstance
    {
        public CodexInstance(string name, string imageName, DateTime startUtc, Address discoveryEndpoint, Address apiEndpoint, Address listenEndpoint, EthAccount? ethAccount, Address? metricsEndpoint)
        {
            Name = name;
            ImageName = imageName;
            StartUtc = startUtc;
            DiscoveryEndpoint = discoveryEndpoint;
            ApiEndpoint = apiEndpoint;
            ListenEndpoint = listenEndpoint;
            EthAccount = ethAccount;
            MetricsEndpoint = metricsEndpoint;
        }

        public string Name { get; }
        public string ImageName { get; }
        public DateTime StartUtc { get; }
        public Address DiscoveryEndpoint { get; }
        public Address ApiEndpoint { get; }
        public Address ListenEndpoint { get; }
        public EthAccount? EthAccount { get; }
        public Address? MetricsEndpoint { get; }
    }

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
