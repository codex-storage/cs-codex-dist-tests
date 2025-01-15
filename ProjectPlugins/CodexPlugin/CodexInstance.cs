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
        void DeleteDataDirFolder();
        EthAccount? GetEthAccount();
        Address? GetMetricsEndpoint();
    }

    public class CodexContainerInstance : ICodexInstance
    {
        private readonly RunningContainer container;
        private readonly IPluginTools tools;
        private readonly ILog log;
        private readonly Address? metricsAddress = null;
        private readonly EthAccount? ethAccount = null;

        public CodexContainerInstance(IPluginTools tools, ILog log, RunningPod pod)
        {
            container = pod.Containers.Single();
            this.tools = tools;
            this.log = log;
            Name = container.Name;
            ImageName = container.Recipe.Image;
            StartUtc = container.Recipe.RecipeCreatedUtc;

            DiscoveryEndpoint = container.GetAddress(CodexContainerRecipe.DiscoveryPortTag);
            ApiEndpoint = container.GetAddress(CodexContainerRecipe.ApiPortTag);
            ListenEndpoint = container.GetAddress(CodexContainerRecipe.ListenPortTag);

            if (pod.StartupConfig.Get<CodexSetup>().MetricsEnabled)
            {
                metricsAddress = container.GetAddress(CodexContainerRecipe.MetricsPortTag);
            }
            ethAccount = container.Recipe.Additionals.Get<EthAccount>();
        }

        public string Name { get; }
        public string ImageName { get; }
        public DateTime StartUtc { get; }
        public Address DiscoveryEndpoint { get; }
        public Address ApiEndpoint { get; }
        public Address ListenEndpoint { get; }

        public void DeleteDataDirFolder()
        {
            try
            {
                var dataDirVar = container.Recipe.EnvVars.Single(e => e.Name == "CODEX_DATA_DIR");
                var dataDir = dataDirVar.Value;
                var workflow = tools.CreateWorkflow();
                workflow.ExecuteCommand(container, "rm", "-Rfv", $"/codex/{dataDir}/repo");
                log.Log("Deleted repo folder.");
            }
            catch (Exception e)
            {
                log.Log("Unable to delete repo folder: " + e);
            }
        }

        public EthAccount? GetEthAccount()
        {
            return ethAccount;
        }

        public Address? GetMetricsEndpoint()
        {
            return metricsAddress;
        }
    }
}
