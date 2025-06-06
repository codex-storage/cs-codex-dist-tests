﻿using CodexClient.Hooks;
using Logging;
using OverwatchTranscript;
using Utils;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexTranscriptWriter : ICodexHooksProvider
    {
        private const string CodexHeaderKey = "cdx_h";
        private readonly ILog log;
        private readonly CodexTranscriptWriterConfig config;
        private readonly ITranscriptWriter writer;
        private readonly CodexLogConverter converter;
        private readonly IdentityMap identityMap = new IdentityMap();
        private readonly KademliaPositionFinder positionFinder = new KademliaPositionFinder();

        public CodexTranscriptWriter(ILog log, CodexTranscriptWriterConfig config, ITranscriptWriter transcriptWriter)
        {
            this.log = log;
            this.config = config;
            writer = transcriptWriter;
            converter = new CodexLogConverter(writer, config, identityMap);
        }

        public void FinalizeWriter()
        {
            log.Log("Finalizing Codex transcript...");

            writer.AddHeader(CodexHeaderKey, CreateCodexHeader());
            writer.Write(GetOutputFullPath());

            log.Log("Done");
        }

        private string GetOutputFullPath()
        {
            var outputPath = Path.GetDirectoryName(log.GetFullName());
            if (outputPath == null) throw new Exception("Logfile path is null");
            var filename = Path.GetFileNameWithoutExtension(log.GetFullName());
            if (string.IsNullOrEmpty(filename)) throw new Exception("Logfile name is null or empty");
            var outputFile = Path.Combine(outputPath, filename + "_" + config.OutputPath);
            if (!outputFile.EndsWith(".owts")) outputFile += ".owts";
            return outputFile;
        }

        public ICodexNodeHooks CreateHooks(string nodeName)
        {
            nodeName = Str.Between(nodeName, "'", "'");
            return new CodexNodeTranscriptWriter(writer, identityMap, nodeName);
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
                NodeIdentity = -1,
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
                Nodes = positionFinder.DeterminePositions(identityMap.Get())
            };
        }

        private bool IsCodexLog(IDownloadedLog log)
        {
            return log.GetLinesContaining("Run Codex node").Any();
        }
    }
}
