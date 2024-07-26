using CodexPlugin.OverwatchSupport.LineConverters;
using Core;
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
            var runner = new ConversionRunner(writer, peerId);
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
        private readonly string peerId;
        private readonly ILineConverter[] converters = new ILineConverter[]
        {
            new BlockReceivedLineConverter()
        };

        public ConversionRunner(ITranscriptWriter writer, string peerId)
        {
            this.writer = writer;
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

        public void AddEvent(DateTime utc, Action<OverwatchCodexEvent> action)
        {
            var e = new OverwatchCodexEvent
            {
                PeerId = peerId,
            };
            action(e);
            writer.Add(utc, e);
        }

        private void ProcessLine(string line, ILineConverter converter)
        {
            if (!line.Contains(converter.Interest)) return;

            var codexLine = CodexLogLine.Parse(line);
            if (codexLine == null) throw new Exception("Unable to parse required line");

            converter.Process(codexLine, (action) =>
            {
                AddEvent(codexLine.TimestampUtc, action);
            });
        }
    }

    public interface ILineConverter
    {
        string Interest { get; }
        void Process(CodexLogLine line, Action<Action<OverwatchCodexEvent>> addEvent);
    }
}
