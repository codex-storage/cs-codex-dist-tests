namespace CodexDistTestCore
{
    public interface IMetricsAccess
    {
        int GetMostRecentInt(string metricName, IOnlineCodexNode node);
    }

    public class MetricsAccess : IMetricsAccess
    {
        public int GetMostRecentInt(string metricName, IOnlineCodexNode node)
        {
            return 0;
        }
    }
}
