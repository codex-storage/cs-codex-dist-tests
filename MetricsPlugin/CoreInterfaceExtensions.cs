using Core;
using KubernetesWorkflow;

namespace MetricsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningContainer StartMetricsCollector(this CoreInterface ci, RunningContainers[] scrapeTargets)
        {
            return null!;// Plugin(ci).StartMetricsCollector(scrapeTargets);
        }

        private static MetricsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<MetricsPlugin>();
        }
    }
}
