using KubernetesWorkflow;

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