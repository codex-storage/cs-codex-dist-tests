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
            var peerId = DeterminPeerId(log);
            var runner = new ConversionRunner(writer, nameIdMap, log.ContainerName, peerId);
            runner.Run(log);
        }

        private string DeterminPeerId(IDownloadedLog log)
        {
            // We have to use a look-up map to match the node name to its peerId,
            // because the Codex logging never prints the peerId in full.
            // After we find it, we confirm it be looking for the shortened version.

            // Expected string:
            // Downloading container log for '<Downloader1>'
            var nameLine = log.FindLinesThatContain("Downloading container log for").First();
            var name = Str.Between(nameLine, "'", "'");

            var peerId = nameIdMap.GetPeerId(name);
            var shortPeerId = CodexUtils.ToShortId(peerId);

            // Look for "Started codex node" line to confirm peerId.
            var startedLine = log.FindLinesThatContain("Started codex node").Single();
            var started = CodexLogLine.Parse(startedLine)!;
            var foundId = started.Attributes["id"];

            if (foundId != shortPeerId) throw new Exception("PeerId from name-lookup did not match PeerId found in codex-started log line.");

            return peerId;
        }
    }

    public class ConversionRunner
    {
        private readonly ITranscriptWriter writer;
        private readonly NameIdMap nameIdMap;
        private readonly string name;
        private readonly string peerId;
        private readonly ILineConverter[] converters = new ILineConverter[]
        {
            new BlockReceivedLineConverter(),
            new BootstrapLineConverter(),
            new DialSuccessfulLineConverter(),
            new PeerDroppedLineConverter()
        };

        public ConversionRunner(ITranscriptWriter writer, NameIdMap nameIdMap, string name, string peerId)
        {
            this.name = name;
            this.writer = writer;
            this.nameIdMap = nameIdMap;
            this.peerId = peerId;
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
                PeerId = peerId,
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
