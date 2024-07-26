using CodexPlugin.Hooks;
using Core;
using OverwatchTranscript;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexTranscriptWriter : ICodexHooksProvider
    {
        private readonly ITranscriptWriter transcriptWriter;

        public CodexTranscriptWriter(ITranscriptWriter transcriptWriter)
        {
            this.transcriptWriter = transcriptWriter;
        }

        public void Finalize(string outputFilepath)
        {
            transcriptWriter.Write(outputFilepath);
        }

        public ICodexNodeHooks CreateHooks(string nodeName)
        {
            return new CodexNodeTranscriptWriter(transcriptWriter, nodeName);
        }

        public void ProcessLogs(IDownloadedLog[] downloadedLogs)
        {
            // which logs to which nodes?
            // nodeIDs, peerIDs needed.
        }
    }

    public class CodexNodeTranscriptWriter : ICodexNodeHooks
    {
        private readonly ITranscriptWriter writer;
        private readonly string name;
        private string peerId = string.Empty;

        public CodexNodeTranscriptWriter(ITranscriptWriter writer, string name)
        {
            this.writer = writer;
            this.name = name;
        }

        public void OnNodeStarted(string peerId, string image)
        {
            this.peerId = peerId;
            WriteCodexEvent(e =>
            {
                e.NodeStarted = new NodeStartedEvent
                {
                    Name = name,
                    Image = image,
                    Args = string.Empty
                };
            });
        }

        public void OnNodeStopping()
        {
            WriteCodexEvent(e =>
            {
                e.NodeStopped = new NodeStoppedEvent
                { 
                    Name = name
                };
            });
        }

        public void OnFileDownloaded(ContentId cid)
        {
            WriteCodexEvent(e =>
            {
                e.FileDownloaded = new FileDownloadedEvent
                {
                    Cid = cid.Id
                };
            });
        }

        public void OnFileUploaded(ContentId cid)
        {
            WriteCodexEvent(e =>
            {
                e.FileUploaded = new FileUploadedEvent
                { 
                    Cid = cid.Id
                };
            });
        }

        private void WriteCodexEvent(Action<OverwatchCodexEvent> action)
        {
            if (string.IsNullOrEmpty(peerId)) throw new Exception("PeerId required");

            var e = new OverwatchCodexEvent
            {
                PeerId = peerId
            };
            action(e);

            writer.Add(DateTime.UtcNow, e);
        }
    }
}
