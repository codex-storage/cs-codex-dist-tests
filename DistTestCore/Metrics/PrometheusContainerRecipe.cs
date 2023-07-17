using KubernetesWorkflow;

namespace DistTestCore.Metrics
{
    public class PrometheusContainerRecipe : ContainerRecipeFactory
    {
        public override string Image { get; }

        public PrometheusContainerRecipe()
        {
            Image = "thatbenbierens/prometheus-envconf:latest";
        }

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<PrometheusStartupConfig>();

            AddExposedPortAndVar("PROM_PORT");
            AddEnvVar("PROM_CONFIG", config.PrometheusConfigBase64);
        }
    }
}
