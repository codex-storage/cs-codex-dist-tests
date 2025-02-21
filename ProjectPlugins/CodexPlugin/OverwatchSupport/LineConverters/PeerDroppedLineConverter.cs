using CodexClient;

namespace CodexPlugin.OverwatchSupport.LineConverters
{
    public class PeerDroppedLineConverter : ILineConverter
    {
        public string Interest => "Dropping peer";

        public void Process(CodexLogLine line, Action<Action<OverwatchCodexEvent>> addEvent)
        {
            var peerId = line.Attributes["peer"];

            addEvent(e =>
            {
                e.PeerDropped = new PeerDroppedEvent
                {
                    DroppedPeerId = peerId
                };
            });
        }
    }
}
