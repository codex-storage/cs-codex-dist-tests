namespace MarketInsights
{
    public class AppState
    {
        public AppState(Configuration config)
        {
            Config = config;
        }

        public MarketOverview MarketOverview { get; set; } = new ();
        public Configuration Config { get; }
    }
}
