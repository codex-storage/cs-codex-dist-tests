namespace OverwatchTranscript
{
    [Serializable]
    public class OverwatchTranscript
    {
        public OverwatchHeader Header { get; set; } = new();
        public OverwatchMoment[] Moments { get; set; } = Array.Empty<OverwatchMoment>();
    }

    [Serializable]
    public class OverwatchHeader
    {
        public OverwatchCommonHeader Common { get; set; } = new();
        public OverwatchHeaderEntry[] Entries { get; set; } = Array.Empty<OverwatchHeaderEntry>();
    }

    [Serializable]
    public class OverwatchCommonHeader
    {
        public long NumberOfEvents { get; set; }
        public DateTime EarliestUct { get; set; }
        public DateTime LatestUtc { get; set; }
    }

    [Serializable]
    public class OverwatchHeaderEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    [Serializable]
    public class OverwatchMoment
    {
        public DateTime Utc { get; set; }
        public OverwatchEvent[] Events { get; set; } = Array.Empty<OverwatchEvent>();
    }

    [Serializable]
    public class OverwatchEvent
    {
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
