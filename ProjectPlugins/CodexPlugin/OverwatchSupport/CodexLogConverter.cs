using CodexPlugin.OverwatchSupport.LineConverters;
using KubernetesWorkflow;
using OverwatchTranscript;
using Utils;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexLogConverter
    {
        private readonly ITranscriptWriter writer;
        private readonly NameIdMap nameIdMap;

        public CodexLogConverter(ITranscriptWriter writer, NameIdMap nameIdMap)
        {
            this.writer = writer;
            this.nameIdMap = nameIdMap;
        }

        public void ProcessLog(IDownloadedLog log)
        {
            var peerId = GetIdentity(log);
            var runner = new ConversionRunner(writer, nameIdMap, log.ContainerName, peerId);
            runner.Run(log);
        }

        private CodexNodeIdentity GetIdentity(IDownloadedLog log)
        {
            var name = DetermineName(log);

            // We have to use a look-up map to match the node name to its peerId and nodeId,
            // because the Codex logging never prints the id in full.
            // After we find it, we confirm it be looking for the shortened version.
            var peerId = DeterminPeerId(name, log);
            var nodeId = DeterminNodeId(name, log);

            return new CodexNodeIdentity
            {
                PeerId = peerId,
                NodeId = nodeId
            };
        }

        private string DeterminNodeId(string name, IDownloadedLog log)
        {
            var nodeId = nameIdMap.GetId(name).NodeId;
            var shortNodeId = CodexUtils.ToNodeIdShortId(nodeId);

            // Look for "Starting discovery node" line to confirm nodeId.
            var startedLine = log.FindLinesThatContain("Starting discovery node").Single();
            var started = CodexLogLine.Parse(startedLine)!;
            var foundId = started.Attributes["node"];

            if (foundId != shortNodeId) throw new Exception("NodeId from name-lookup did not match NodeId found in codex-started log line.");

            return nodeId;
        }

        private string DeterminPeerId(string name, IDownloadedLog log)
        {
            var peerId = nameIdMap.GetId(name).PeerId;
            var shortPeerId = CodexUtils.ToShortId(peerId);

            // Look for "Started codex node" line to confirm peerId.
            var startedLine = log.FindLinesThatContain("Started codex node").Single();
            var started = CodexLogLine.Parse(startedLine)!;
            var foundId = started.Attributes["id"];

            if (foundId != shortPeerId) throw new Exception("PeerId from name-lookup did not match PeerId found in codex-started log line.");

            return peerId;
        }

        private string DetermineName(IDownloadedLog log)
        {
            // Expected string:
            // Downloading container log for '<Downloader1>'
            var nameLine = log.FindLinesThatContain("Downloading container log for").First();
            return Str.Between(nameLine, "'", "'");
        }
    }

    public class ConversionRunner
    {
        private readonly ITranscriptWriter writer;
        private readonly NameIdMap nameIdMap;
        private readonly string name;
        private readonly CodexNodeIdentity nodeIdentity;
        private readonly ILineConverter[] converters = new ILineConverter[]
        {
            new BlockReceivedLineConverter(),
            new BootstrapLineConverter(),
            new DialSuccessfulLineConverter(),
            new PeerDroppedLineConverter()
        };

        public ConversionRunner(ITranscriptWriter writer, NameIdMap nameIdMap, string name, CodexNodeIdentity nodeIdentity)
        {
            this.name = name;
            this.nodeIdentity = nodeIdentity;
            this.writer = writer;
            this.nameIdMap = nameIdMap;
        }

        public void Run(IDownloadedLog log)
        {
            log.IterateLines(line =>
            {
                foreach (var converter in converters)
                {
                    ProcessLine(line, converter);
                }
            });
        }

        private void AddEvent(DateTime utc, Action<OverwatchCodexEvent> action)
        {
            var e = new OverwatchCodexEvent
            {
                Name = name,
                Identity = nodeIdentity,
            };
            action(e);

            e.Write(utc, writer);
        }

        private void ProcessLine(string line, ILineConverter converter)
        {
            if (!line.Contains(converter.Interest)) return;

            var codexLine = CodexLogLine.Parse(line);

            if (codexLine == null) throw new Exception("Unable to parse required line");
            EnsureFullIds(codexLine);

            converter.Process(codexLine, (action) =>
            {
                AddEvent(codexLine.TimestampUtc, action);
            });
        }

        private void EnsureFullIds(CodexLogLine codexLine)
        {
            // The issue is: node IDs occure both in full and short version.
            // Downstream tools will assume that a node ID string-equals its own ID.
            // So we replace all shortened IDs we can find with their full ones.

            // Usually, the shortID appears as the entire string of an attribute:
            // "peerId=123*567890"
            // But sometimes, it is part of a larger string:
            // "thing=abc:123*567890,def"
            
            foreach (var pair in codexLine.Attributes)
            {
                if (pair.Value.Contains("*"))
                {
                    codexLine.Attributes[pair.Key] = nameIdMap.ReplaceShortIds(pair.Value);
                }
            }
        }
    }

    public interface ILineConverter
    {
        string Interest { get; }
        void Process(CodexLogLine line, Action<Action<OverwatchCodexEvent>> addEvent);
    }
}
