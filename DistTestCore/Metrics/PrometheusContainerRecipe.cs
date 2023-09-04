using KubernetesWorkflow;

namespace DistTestCore.Metrics
{
    public class PrometheusContainerRecipe : DefaultContainerRecipe
    {
        public override string AppName => "prometheus";
        public override string Image => "codexstorage/dist-tests-prometheus:latest";

        protected override void InitializeRecipe(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<PrometheusStartupConfig>();

            AddExposedPortAndVar("PROM_PORT");
            AddEnvVar("PROM_CONFIG", config.PrometheusConfigBase64);
        }
    }
}
