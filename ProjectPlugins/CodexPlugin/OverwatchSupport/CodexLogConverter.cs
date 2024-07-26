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


        }

        private string DeterminPeerId(IDownloadedLog log)
        {
            // We have to use a look-up map to match the node name to its peerId,
            // because the Codex logging never prints the peerId in full.
            // After we find it, we confirm it be looking for the shortened version.

            // Expected string:
            // Downloading container log for '<Downloader1>'
            var nameLine = log.FindLinesThatContain("Downloading container log for").Single();
            var name = Str.Between(nameLine, "'<", ">'");

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
}
