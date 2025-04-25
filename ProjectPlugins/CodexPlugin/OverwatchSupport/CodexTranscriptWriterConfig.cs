namespace CodexPlugin.OverwatchSupport
{
    public class CodexTranscriptWriterConfig
    {
        public CodexTranscriptWriterConfig(bool includeBlockReceivedEvents)
        {
            IncludeBlockReceivedEvents = includeBlockReceivedEvents;
        }

        public bool IncludeBlockReceivedEvents { get; }
    }
}
