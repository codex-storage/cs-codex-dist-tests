namespace CodexPlugin.OverwatchSupport
{
    public class CodexTranscriptWriterConfig
    {
        public CodexTranscriptWriterConfig(string outputPath, bool includeBlockReceivedEvents)
        {
            OutputPath = outputPath;
            IncludeBlockReceivedEvents = includeBlockReceivedEvents;
        }

        public string OutputPath { get; }
        public bool IncludeBlockReceivedEvents { get; }
    }
}
