using CodexPlugin.Hooks;
using Core;
using OverwatchTranscript;
using Utils;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexTranscriptWriter : ICodexHooksProvider
    {
        private const string CodexHeaderKey = "cdx_h";
        private readonly ITranscriptWriter writer;
        private readonly CodexLogConverter converter;
        private readonly NameIdMap nameIdMap = new NameIdMap();

        public CodexTranscriptWriter(ITranscriptWriter transcriptWriter)
        {
            writer = transcriptWriter;
            converter = new CodexLogConverter(writer, nameIdMap);
        }

        public void Finalize(string outputFilepath)
        {
            writer.AddHeader(CodexHeaderKey, CreateCodexHeader());

            writer.Write(outputFilepath);

            we need:
            total number of events
            min max utc, time range
            total number of codex nodes
        }

        public ICodexNodeHooks CreateHooks(string nodeName)
        {
            nodeName = Str.Between(nodeName, "'", "'");
            return new CodexNodeTranscriptWriter(writer, nameIdMap, nodeName);
        }

        public void IncludeFile(string filepath)
        {
            writer.IncludeArtifact(filepath);   
        }

        public void ProcessLogs(IDownloadedLog[] downloadedLogs)
        {
            foreach (var log in downloadedLogs)
            {
                writer.IncludeArtifact(log.GetFilepath());
                // Not all of these logs are necessarily Codex logs.
                // Check, and process only the Codex ones.
                if (IsCodexLog(log))
                {
                    converter.ProcessLog(log);
                }
            }
        }

        public void AddResult(bool success, string result)
        {
            writer.Add(DateTime.UtcNow, new OverwatchCodexEvent
            {
                PeerId = string.Empty,
                ScenarioFinished = new ScenarioFinishedEvent
                {
                    Success = success,
                    Result = result
                }
            });
        }

        private OverwatchCodexHeader CreateCodexHeader()
        {
            return new OverwatchCodexHeader
            {
                TotalNumberOfNodes = nameIdMap.Size
            };
        }

        private bool IsCodexLog(IDownloadedLog log)
        {
            return log.GetLinesContaining("Run Codex node").Any();
        }
    }

    public class CodexNodeTranscriptWriter : ICodexNodeHooks
    {
        private readonly ITranscriptWriter writer;
        private readonly NameIdMap nameIdMap;
        private readonly string name;
        private string peerId = string.Empty;

        public CodexNodeTranscriptWriter(ITranscriptWriter writer, NameIdMap nameIdMap, string name)
        {
            this.writer = writer;
            this.nameIdMap = nameIdMap;
            this.name = name;
        }

        public void OnNodeStarting(DateTime startUtc, string image)
        {
            WriteCodexEvent(startUtc, e =>
            {
                e.NodeStarting = new NodeStartingEvent
                {
                    Name = name,
                    Image = image
                };
            });
        }

        public void OnNodeStarted(string peerId)
        {
            this.peerId = peerId;
            nameIdMap.Add(name, peerId);
            WriteCodexEvent(e =>
            {
                e.NodeStarted = new NodeStartedEvent
                {
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
            WriteCodexEvent(DateTime.UtcNow, action);
        }

        private void WriteCodexEvent(DateTime utc, Action<OverwatchCodexEvent> action)
        {
            var e = new OverwatchCodexEvent
            {
                PeerId = peerId
            };
            action(e);

            writer.Add(utc, e);
        }
    }
}
