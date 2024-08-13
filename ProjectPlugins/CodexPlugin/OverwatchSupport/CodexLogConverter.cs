using CodexPlugin.OverwatchSupport.LineConverters;
using KubernetesWorkflow;
using OverwatchTranscript;
using Utils;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexLogConverter
    {
        private readonly ITranscriptWriter writer;
        private readonly IdentityMap identityMap;

        public CodexLogConverter(ITranscriptWriter writer, IdentityMap identityMap)
        {
            this.writer = writer;
            this.identityMap = identityMap;
        }

        public void ProcessLog(IDownloadedLog log)
        {
            var name = DetermineName(log);
            var identityIndex = identityMap.GetIndex(name);
            var runner = new ConversionRunner(writer, identityMap, log.ContainerName, identityIndex);
            runner.Run(log);
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
        private readonly IdentityMap nameIdMap;
        private readonly string name;
        private readonly int nodeIdentityIndex;
        private readonly ILineConverter[] converters = new ILineConverter[]
        {
            new BlockReceivedLineConverter(),
            new BootstrapLineConverter(),
            new DialSuccessfulLineConverter(),
            new PeerDroppedLineConverter()
        };

        public ConversionRunner(ITranscriptWriter writer, IdentityMap nameIdMap, string name, int nodeIdentityIndex)
        {
            this.name = name;
            this.nodeIdentityIndex = nodeIdentityIndex;
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
                NodeIdentity = nodeIdentityIndex,
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
