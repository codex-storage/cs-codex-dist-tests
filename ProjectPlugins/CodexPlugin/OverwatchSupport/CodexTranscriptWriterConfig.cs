namespace CodexPlugin.OverwatchSupport
{
    public class CodexTranscriptWriterConfig
    {
        public CodexTranscriptWriterConfig(string outputFilename, bool includeBlockReceivedEvents)
        {
            OutputFilename = outputFilename;
            IncludeBlockReceivedEvents = includeBlockReceivedEvents;
        }

        public string OutputFilename { get; }
        public bool IncludeBlockReceivedEvents { get; }
    }
}
