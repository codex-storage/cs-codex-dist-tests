using CodexPlugin;
using CodexPlugin.OverwatchSupport;
using OverwatchTranscript;

namespace TranscriptAnalysis.Receivers
{
    public class NodesDegree : BaseReceiver<OverwatchCodexEvent>
    {
        private readonly Dictionary<string, Dictionary<string, int>> dials = new Dictionary<string, Dictionary<string, int>>();

        public override string Name => "NodesDegree";

        public override void Receive(ActivateEvent<OverwatchCodexEvent> @event)
        {
            if (@event.Payload.DialSuccessful != null)
            {
                AddDial(@event.Payload.PeerId, @event.Payload.DialSuccessful.TargetPeerId);
            }
        }

        private void AddDial(string peerId, string targetPeerId)
        {
            if (!dials.ContainsKey(peerId))
            {
                dials.Add(peerId, new Dictionary<string, int>());
            }

            var d = dials[peerId];
            if (!d.ContainsKey(targetPeerId))
            {
                d.Add(targetPeerId, 1);
            }
            else
            {
                d[targetPeerId]++;
            }
        }

        public override void Finish()
        {
            var numNodes = dials.Keys.Count;
            var redials = dials.Values.Count(t => t.Values.Any(nd => nd > 1));

            var min = dials.Values.Min(t => t.Count);
            var avg = dials.Values.Average(t => t.Count);
            var max = dials.Values.Max(t => t.Count);

            Log($"Nodes: {numNodes} - Degrees: min:{min} avg:{avg} max:{max} - Redials: {redials}");
        }
    }
}
