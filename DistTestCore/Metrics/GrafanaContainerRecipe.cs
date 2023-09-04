using KubernetesWorkflow;

namespace DistTestCore.Metrics
{
    public class GrafanaContainerRecipe : DefaultContainerRecipe
    {
        public override string AppName => "grafana";
        public override string Image => "grafana/grafana-oss:10.0.3";

        public const string DefaultAdminUser = "adminium";
        public const string DefaultAdminPassword = "passwordium";

        protected override void InitializeRecipe(StartupConfig startupConfig)
        {
            AddExposedPort(3000);

            AddEnvVar("GF_AUTH_ANONYMOUS_ENABLED", "true");
            AddEnvVar("GF_AUTH_ANONYMOUS_ORG_NAME", "Main Org.");
            AddEnvVar("GF_AUTH_ANONYMOUS_ORG_ROLE", "Editor");

            AddEnvVar("GF_SECURITY_ADMIN_USER", DefaultAdminUser);
            AddEnvVar("GF_SECURITY_ADMIN_PASSWORD", DefaultAdminPassword);
        }
    }
}
