namespace OverwatchTranscript
{
    [Serializable]
    public class OverwatchTranscript
    {
        public OverwatchHeader Header { get; set; } = new();
        public OverwatchEvent[] Events { get; set; } = Array.Empty<OverwatchEvent>();
    }

    [Serializable]
    public class OverwatchHeader
    {
        public OverwatchHeaderEntry[] Entries { get; set; } = Array.Empty<OverwatchHeaderEntry>();
    }

    [Serializable]
    public class OverwatchHeaderEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    [Serializable]
    public class OverwatchEvent
    {
        public DateTime Utc { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
