using KubernetesWorkflow;

namespace DistTestCore.Metrics
{
    public class GrafanaContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "grafana";
        public override string Image => "grafana/grafana-oss:10.0.3";

        protected override void Initialize(StartupConfig startupConfig)
        {
            //var config = startupConfig.Get<PrometheusStartupConfig>();

            //AddExposedPortAndVar("PROM_PORT");
            AddExposedPort(3000);
            //AddEnvVar("PROM_CONFIG", config.PrometheusConfigBase64);

            // [auth.anonymous]
            //  enabled = true
            //GF_<SectionName>_<KeyName>__FILE

            //AddEnvVar("GF_AUTH_ANONYMOUS_ENABLED", "true");
            //AddEnvVar("GF_AUTH_DISABLE_LOGIN_FORM", "true");

            //[auth]
            //disable_login_form = true
        }
    }
}
