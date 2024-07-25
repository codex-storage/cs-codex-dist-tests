using Core;
using OverwatchTranscript;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexTranscriptWriter
    {
        private readonly ITranscriptWriter transcriptWriter;

        public CodexTranscriptWriter(ITranscriptWriter transcriptWriter)
        {
            this.transcriptWriter = transcriptWriter;
        }

        public void ProcessLogs(IDownloadedLog[] downloadedLogs)
        {
            throw new NotImplementedException();
        }
    }
}
