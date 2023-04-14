using KubernetesWorkflow;

namespace DistTestCore.Metrics
{
    public class PrometheusContainerRecipe : ContainerRecipeFactory
    {
        public const string DockerImage = "thatbenbierens/prometheus-envconf:latest";

        protected override string Image => DockerImage;

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<PrometheusStartupConfig>();

            AddExposedPortAndVar("PROM_PORT");
            AddEnvVar("PROM_CONFIG", config.PrometheusConfigBase64);
        }
    }
}
