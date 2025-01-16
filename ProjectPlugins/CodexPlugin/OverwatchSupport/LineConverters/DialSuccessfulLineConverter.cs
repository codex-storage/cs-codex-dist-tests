using CodexClient;

namespace CodexPlugin.OverwatchSupport.LineConverters
{
    public class DialSuccessfulLineConverter : ILineConverter
    {
        public string Interest => "Dial successful";

        public void Process(CodexLogLine line, Action<Action<OverwatchCodexEvent>> addEvent)
        {
            var peerId = line.Attributes["peerId"];

            addEvent(e =>
            {
                e.DialSuccessful = new PeerDialSuccessfulEvent
                {
                    TargetPeerId = peerId
                };
            });
        }
    }
}
