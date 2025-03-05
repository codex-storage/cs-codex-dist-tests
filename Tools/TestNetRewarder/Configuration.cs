using ArgsUniform;

namespace TestNetRewarder
{
    public class Configuration
    {
        private readonly DateTime AppStartUct = DateTime.UtcNow;

        [Uniform("datapath", "dp", "DATAPATH", true, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";

        [Uniform("discordbot-host", "dh", "DISCORDBOTHOST", true, "http address of the discord bot.")]
        public string DiscordHost { get; set; } = "host";

        [Uniform("discordbot-port", "dp", "DISCORDBOTPORT", true, "port number of the discord bot reward API.")]
        public int DiscordPort { get; set; } = 31080;

        [Uniform("interval-minutes", "im", "INTERVALMINUTES", true, "time in minutes between reward updates.")]
        public int IntervalMinutes { get; set; } = 15;

        [Uniform("relative-history", "rh", "RELATIVEHISTORY", false, "Number of seconds into the past (from app start) that checking of chain history will start. Default: 3 hours ago.")]
        public int RelativeHistorySeconds { get; set; } = 3600 * 3;

        [Uniform("market-insights", "mi", "MARKETINSIGHTS", false, "Semi-colon separated integers. Each represents a multiple of intervals, for which a market insights average will be generated.")]
        public string MarketInsights { get; set; } = "1;96";

        [Uniform("events-overview", "eo", "EVENTSOVERVIEW", false, "When greater than zero, chain event summary will be generated.")]
        public int CreateChainEventsOverview { get; set; } = 1;

        [Uniform("proof-period-reports", "ppr", "PROOFPERIODREPORTS", false, "When greater than zero, chain event summary will include period reports of the proving system.")]
        public int ShowProofPeriodReports { get; set; } = 1;

        [Uniform("proof-submitted-events", "pse", "PROOFSUBMITTEDEVENTS", false, "When greater than zero, chain event summary will include proof-submitted events.")]
        public int ShowProofSubmittedEvents { get; set; } = 0; // Defaulted to zero, aprox 7 to 10 such events every 2 minutes in testnet (from autoclient alone!)

        public string LogPath
        {
            get
            {
                return Path.Combine(DataPath, "logs");
            }
        }

        public TimeSpan Interval
        {
            get
            {
                return TimeSpan.FromMinutes(IntervalMinutes);
            }
        }

        public DateTime HistoryStartUtc
        {
            get
            {
                return AppStartUct - TimeSpan.FromSeconds(RelativeHistorySeconds);
            }
        }
    }
}
