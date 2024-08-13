using CodexPlugin;
using CodexPlugin.OverwatchSupport;
using OverwatchTranscript;

namespace TranscriptAnalysis.Receivers
{
    public class LogReplaceReceiver : BaseReceiver<OverwatchCodexEvent>
    {
        public override string Name => "LogReplacer";

        private readonly List<string> seen = new List<string>();

        public override void Receive(ActivateEvent<OverwatchCodexEvent> @event)
        {
            if (!seen.Contains(@event.Payload.Identity.PeerId))
            {
                seen.Add(@event.Payload.Identity.PeerId);

                log.AddStringReplace(@event.Payload.Identity.PeerId, @event.Payload.Name);
                log.AddStringReplace(CodexUtils.ToShortId(@event.Payload.Identity.PeerId), @event.Payload.Name);
            }
        }

        public override void Finish()
        {
        }
    }
}
