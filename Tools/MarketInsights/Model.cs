namespace MarketInsights
{
    public class MarketOverview
    {
        /// <summary>
        /// Moment when overview was last updated.
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; }

        public MarketTimeSegment[] TimeSegments { get; set; } = Array.Empty<MarketTimeSegment>();
    }

    /// <summary>
    /// Segment of time over which market statistics are available.
    /// </summary>
    public class MarketTimeSegment
    {
        /// <summary>
        /// Start of time segment.
        /// </summary>
        public DateTime FromUtc { get; set; }

        /// <summary>
        /// End of time segment.
        /// </summary>
        public DateTime ToUtc { get; set; }

        /// <summary>
        /// Averages over contracts that were submitted during this time segment.
        /// </summary>
        public ContractAverages Submitted { get; set; } = new();

        /// <summary>
        /// Averages over contracts that expired during this time segment.
        /// </summary>
        public ContractAverages Expired { get; set; } = new();

        /// <summary>
        /// Averages over contracts that started during this time segment.
        /// </summary>
        public ContractAverages Started { get; set; } = new();

        /// <summary>
        /// Averages over contracts that finished (succesfully) during this time segment.
        /// </summary>
        public ContractAverages Finished { get; set; } = new();

        /// <summary>
        /// Averages over contracts that failed during this time segment.
        /// </summary>
        public ContractAverages Failed { get; set; } = new();
    }

    public class ContractAverages
    {
        /// <summary>
        /// Number of contracts.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Average price of contracts. (TSTWEI)
        /// </summary>
        public float Price { get; set; }

        /// <summary>
        /// Average size of slots in contracts. (bytes)
        /// </summary>
        public float Size { get; set; }

        /// <summary>
        /// Average duration of contracts. (seconds)
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Average collateral of contracts. (TSTWEI)
        /// </summary>
        public float Collateral { get; set; }

        /// <summary>
        /// Average proof probability of contracts.
        /// </summary>
        public float ProofProbability { get; set; }
    }
}
