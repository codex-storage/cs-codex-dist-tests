namespace DistTestCore.Metrics
{
    public class PrometheusStartupConfig
    {
        public PrometheusStartupConfig(string prometheusConfigBase64)
        {
            PrometheusConfigBase64 = prometheusConfigBase64;
        }

        public string PrometheusConfigBase64 { get; }
    }
}
