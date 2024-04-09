using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace DeployAndRunPlugin
{
    public class DeployAndRunContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "deploy-and-run";
        public override string Image => "thatbenbierens/dist-tests-deployandrun:initial";

        protected override void Initialize(StartupConfig config)
        {
            var setup = config.Get<RunConfig>();

            if (setup.CodexImageOverride != null)
            {
                AddEnvVar("CODEXDOCKERIMAGE", setup.CodexImageOverride);
            }

            AddEnvVar("DNR_REP", setup.Replications.ToString());
            AddEnvVar("DNR_NAME", setup.Name);
            AddEnvVar("DNR_FILTER", setup.Filter);
            AddEnvVar("DNR_DURATION", setup.Duration.TotalSeconds.ToString());

            AddEnvVar("KUBECONFIG", "/opt/kubeconfig.yaml");
            AddEnvVar("LOGPATH", "/var/log/codex-continuous-tests");

            AddVolume(name: "kubeconfig", mountPath: "/opt/kubeconfig.yaml", subPath: "kubeconfig.yaml", secret: "codex-dist-tests-app-kubeconfig");
            AddVolume(name: "logs", mountPath: "/var/log/codex-continuous-tests", hostPath: "/var/log/codex-continuous-tests");
        }
    }

    public class RunConfig
    {
        public RunConfig(string name, string filter, TimeSpan duration, int replications, string? codexImageOverride = null)
        {
            Name = name;
            Filter = filter;
            Duration = duration;
            Replications = replications;
            CodexImageOverride = codexImageOverride;
        }

        public string Name { get; }
        public string Filter { get; }
        public TimeSpan Duration { get; }
        public int Replications { get; }
        public string? CodexImageOverride { get; }
    }
}