using Logging;

namespace MarketInsights
{
    public class AppState
    {
        public AppState(Configuration config)
        {
            Config = config;
        }

        public bool Realtime { get; set; }
        public MarketOverview MarketOverview { get; set; } = new();
        public Configuration Config { get; }
        public ILog Log { get; } = new ConsoleLog();
    }
}
