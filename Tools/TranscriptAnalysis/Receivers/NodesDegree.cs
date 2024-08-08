using CodexPlugin;
using CodexPlugin.OverwatchSupport;
using OverwatchTranscript;

namespace TranscriptAnalysis.Receivers
{
    public class NodesDegree : BaseReceiver<OverwatchCodexEvent>
    {
        public class Dial
        {
            public Dial(Node peer, Node target)
            {
                Id = GetLineId(peer.Id, target.Id);
                InitiatedBy.Add(peer);
                Peer = peer;
                Target = target;
            }

            public string Id { get; }
            public int RedialCount => InitiatedBy.Count - 1;
            public List<Node> InitiatedBy { get; } = new List<Node>();
            public Node Peer { get; }
            public Node Target { get; }

            private string GetLineId(string a, string b)
            {
                if (string.Compare(a, b) > 0)
                {
                    return a + b;
                }
                return b + a;
            }
        }

        public class Node
        {
            public Node(string peerId)
            {
                Id = peerId;
            }

            public string Id { get; }
            public List<Dial> Dials { get; } = new List<Dial>();
            public int Degree => Dials.Count;
        }

        private readonly Dictionary<string, Node> dialingNodes = new Dictionary<string, Node>();
        private readonly Dictionary<string, Dial> dials = new Dictionary<string, Dial>();

        public override string Name => "NodesDegree";

        public override void Receive(ActivateEvent<OverwatchCodexEvent> @event)
        {
            if (@event.Payload.DialSuccessful != null)
            {
                AddDial(@event.Payload.PeerId, @event.Payload.DialSuccessful.TargetPeerId);
            }
        }

        public override void Finish()
        {
            var numNodes = dialingNodes.Count;
            var redialOccurances = new OccuranceMap();
            foreach (var dial in dials.Values)
            {
                redialOccurances.Add(dial.RedialCount);
            }
            var degreeOccurances = new OccuranceMap();
            foreach (var node in dialingNodes.Values)
            {
                degreeOccurances.Add(node.Degree);
            }

            Log($"Dialing nodes: {numNodes}");
            Log("Redials:");
            redialOccurances.PrintContinous((i, count) =>
            {
                Log($"{i} redials = {count}x");
            });

            float tot = numNodes;
            degreeOccurances.Print((i, count) =>
            {
                float n = count;
                float p = 100.0f * (n / tot);
                Log($"Degree: {i} = {count}x ({p}%)");
            });
        }

        private void AddDial(string peerId, string targetPeerId)
        {
            peerId = CodexUtils.ToShortId(peerId);
            targetPeerId = CodexUtils.ToShortId(targetPeerId);

            var peer = GetNode(peerId);
            var target = GetNode(targetPeerId); ;

            var dial = new Dial(peer, target);

            if (dials.ContainsKey(dial.Id))
            {
                var d = dials[dial.Id];
                d.InitiatedBy.Add(peer);
                peer.Dials.Add(d);
                target.Dials.Add(d);
            }
            else
            {
                dials.Add(dial.Id, dial);
                peer.Dials.Add(dial);
                target.Dials.Add(dial);
            }
        }

        private Node GetNode(string id)
        {
            if (!dialingNodes.ContainsKey(id))
            {
                dialingNodes.Add(id, new Node(id));
            }
            return dialingNodes[id];
        }
    }
}
