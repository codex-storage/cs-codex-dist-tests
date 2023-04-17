using KubernetesWorkflow;
using Utils;

namespace DistTestCore.Marketplace
{
    public class CodexContractsStarter
    {
        private const string readyString = "Done! Sleeping indefinitely...";
        private readonly TestLifecycle lifecycle;
        private readonly WorkflowCreator workflowCreator;

        public CodexContractsStarter(TestLifecycle lifecycle, WorkflowCreator workflowCreator)
        {
            this.lifecycle = lifecycle;
            this.workflowCreator = workflowCreator;
        }

        public void Start(RunningContainer bootstrapContainer)
        {
            var workflow = workflowCreator.CreateWorkflow();
            var startupConfig = CreateStartupConfig(bootstrapContainer);

            lifecycle.Log.Log("Deploying Codex contracts...");
            var containers = workflow.Start(1, Location.Unspecified, new CodexContractsContainerRecipe(), startupConfig);
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Codex contracts container to be created. Test infra failure.");
            var container = containers.Containers[0];

            WaitUntil(() =>
            {
                var logHandler = new ContractsReadyLogHandler(readyString);
                workflow.DownloadContainerLog(container, logHandler);
                return logHandler.Found;
            });

            lifecycle.Log.Log("Contracts deployed.");
        }

        private void WaitUntil(Func<bool> predicate)
        {
            Time.WaitUntil(predicate, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(1));
        }

        private StartupConfig CreateStartupConfig(RunningContainer bootstrapContainer)
        {
            var startupConfig = new StartupConfig();
            var contractsConfig = new CodexContractsContainerConfig(bootstrapContainer.Pod.Ip, bootstrapContainer.Recipe.GetPortByTag(GethContainerRecipe.HttpPortTag));
            startupConfig.Add(contractsConfig);
            return startupConfig;
        }
    }

    public class ContractsReadyLogHandler : LogHandler
    {
        private readonly string targetString;

        public ContractsReadyLogHandler(string targetString)
        {
            this.targetString = targetString;
        }

        public bool Found { get; private set; }

        protected override void ProcessLine(string line)
        {
            if (line.Contains(targetString)) Found = true;
        }
    }
}
