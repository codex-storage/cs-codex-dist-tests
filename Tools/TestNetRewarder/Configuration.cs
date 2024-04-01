using ArgsUniform;

namespace TestNetRewarder
{
    public class Configuration
    {
        [Uniform("datapath", "dp", "DATAPATH", false, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";

        [Uniform("discordbot-host", "dh", "DISCORDBOTHOST", true, "http address of the discord bot.")]
        public string DiscordHost { get; set; } = "host";

        [Uniform("discordbot-port", "dp", "DISCORDBOTPORT", true, "port number of the discord bot reward API. (31080 by default)")]
        public int DiscordPort { get; set; } = 31080;

        [Uniform("interval-minutes", "im", "INTERVALMINUTES", false, "time in minutes between reward updates. (default 15)")]
        public int IntervalMinutes { get; set; } = 15;

        [Uniform("check-history", "ch", "CHECKHISTORY", true, "Unix epoc timestamp of a moment in history on which processing begins. Required for hosting rewards. Should be 'launch of the testnet'.")]
        public int CheckHistoryTimestamp { get; set; } = 0;

        public string LogPath
        {
            get
            {
                return Path.Combine(DataPath, "logs");
            }
        }
    }
}
