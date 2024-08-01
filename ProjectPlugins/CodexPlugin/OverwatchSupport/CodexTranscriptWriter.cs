using CodexPlugin.Hooks;
using KubernetesWorkflow;
using Logging;
using OverwatchTranscript;
using Utils;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexTranscriptWriter : ICodexHooksProvider
    {
        private const string CodexHeaderKey = "cdx_h";
        private readonly ILog log;
        private readonly ITranscriptWriter writer;
        private readonly CodexLogConverter converter;
        private readonly NameIdMap nameIdMap = new NameIdMap();

        public CodexTranscriptWriter(ILog log, ITranscriptWriter transcriptWriter)
        {
            this.log = log;
            writer = transcriptWriter;
            converter = new CodexLogConverter(writer, nameIdMap);
        }

        public void Finalize(string outputFilepath)
        {
            log.Log("Finalizing Codex transcript...");

            writer.AddHeader(CodexHeaderKey, CreateCodexHeader());
            writer.Write(outputFilepath);

            log.Log("Done");
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
            foreach (var l in downloadedLogs)
            {
                log.Log("Include artifact: " + l.GetFilepath());
                writer.IncludeArtifact(l.GetFilepath());

                // Not all of these logs are necessarily Codex logs.
                // Check, and process only the Codex ones.
                if (IsCodexLog(l))
                {
                    log.Log("Processing Codex log: " + l.GetFilepath());
                    converter.ProcessLog(l);
                }
            }
        }

        public void AddResult(bool success, string result)
        {
            writer.Add(DateTime.UtcNow, new OverwatchCodexEvent
            {
                Name = string.Empty,
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
}
