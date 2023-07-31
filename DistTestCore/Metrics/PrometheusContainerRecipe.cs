using KubernetesWorkflow;

namespace DistTestCore.Metrics
{
    public class PrometheusContainerRecipe : ContainerRecipeFactory
    {
        public override string Image { get; }

        public PrometheusContainerRecipe()
        {
            Image = "codexstorage/dist-tests-prometheus:sha-f97d7fd";
        }

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<PrometheusStartupConfig>();

            AddExposedPortAndVar("PROM_PORT");
            AddEnvVar("PROM_CONFIG", config.PrometheusConfigBase64);
        }
    }
}
