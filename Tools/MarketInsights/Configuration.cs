using ArgsUniform;

namespace MarketInsights
{
    public class Configuration
    {
        [Uniform("interval-minutes", "im", "INTERVALMINUTES", true, "time in minutes between updates.")]
        public int UpdateIntervalMinutes { get; set; } = 10;

        [Uniform("max-random-seconds", "mrs", "MAXRANDOMSECONDS", false, "maximum random number of seconds added to update delay.")]
        public int MaxRandomIntervalSeconds { get; set; } = 120;

        [Uniform("check-history", "ch", "CHECKHISTORY", true, "Unix epoc timestamp of a moment in history on which processing begins. Should be 'launch of the testnet'.")]
        public int CheckHistoryTimestamp { get; set; } = 0;

        /// <summary>
        /// 6 = 1h
        /// 144 = 24h
        /// 2520 = 1 week
        /// 10080 = 4 weeks
        /// </summary>
        [Uniform("timesegments", "ts", "TIMESEGMENTS", false, "Semi-colon separated integers. Each represents a multiple of intervals, for which a market timesegment will be generated.")]
        public string TimeSegments { get; set; } = "6;144;2520;10080";

        [Uniform("fullhistory", "fh", "FULLHISTORY", false, "When not zero, market timesegment for 'entire history' will be included.")]
        public int FullHistory { get; set; } = 1;

        public DateTime HistoryStartUtc
        {
            get
            {
                if (CheckHistoryTimestamp == 0) throw new Exception("'check-history' unix timestamp is required. Set it to the start/launch moment of the testnet.");
                return DateTimeOffset.FromUnixTimeSeconds(CheckHistoryTimestamp).UtcDateTime;
            }
        }
    }
}
