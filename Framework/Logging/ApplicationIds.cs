namespace Logging
{
    public class ApplicationIds
    {
        public ApplicationIds(string codexId, string gethId, string prometheusId, string codexContractsId, string grafanaId)
        {
            CodexId = codexId;
            GethId = gethId;
            PrometheusId = prometheusId;
            CodexContractsId = codexContractsId;
            GrafanaId = grafanaId;
        }

        public string CodexId { get; }
        public string GethId { get; }
        public string PrometheusId { get; }
        public string CodexContractsId { get; }
        public string GrafanaId { get; }
    }
}
