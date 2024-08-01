namespace CodexPlugin.OverwatchSupport.LineConverters
{
    public class PeerDroppedLineConverter : ILineConverter
    {
        public string Interest => "Peer dropped";

        public void Process(CodexLogLine line, Action<Action<OverwatchCodexEvent>> addEvent)
        {
            var peerId = line.Attributes["peerId"];

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
