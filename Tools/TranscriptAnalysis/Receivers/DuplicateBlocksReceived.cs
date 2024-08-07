using CodexPlugin.OverwatchSupport;
using OverwatchTranscript;

namespace TranscriptAnalysis.Receivers
{
    public class DuplicateBlocksReceived : BaseReceiver<OverwatchCodexEvent>
    {
        public override string Name => "BlocksReceived";

        public override void Receive(ActivateEvent<OverwatchCodexEvent> @event)
        {
            if (@event.Payload.BlockReceived != null)
            {
                Handle(@event.Payload, @event.Payload.BlockReceived);
            }
        }

        public override void Finish()
        {
            Log("Number of BlockReceived events seen: " + seen);

            var totalReceived = peerIdBlockAddrCount.Sum(a => a.Value.Sum(p => p.Value));
            var maxRepeats = peerIdBlockAddrCount.Max(a => a.Value.Max(p => p.Value));
            var occurances = new int[maxRepeats + 1];

            foreach (var peerPair in peerIdBlockAddrCount)
            {
                foreach (var pair in peerPair.Value)
                {
                    occurances[pair.Value]++;
                }
            }

            float t = totalReceived;
            for (var i = 1; i < occurances.Length; i++)
            {
                float n = occurances[i];
                float p = 100.0f * (n / t);
                Log($"Block received {i} times = {occurances[i]}x ({p}%)");
            }
        }

        private int seen = 0;
        private readonly Dictionary<string, Dictionary<string, int>> peerIdBlockAddrCount = new Dictionary<string, Dictionary<string, int>>();

        private void Handle(OverwatchCodexEvent payload, BlockReceivedEvent blockReceived)
        {
            var receiverPeerId = payload.PeerId;
            var blockAddress = blockReceived.BlockAddress;
            seen++;

            if (!peerIdBlockAddrCount.ContainsKey(receiverPeerId))
            {
                peerIdBlockAddrCount.Add(receiverPeerId, new Dictionary<string, int>());
            }
            var blockAddCount = peerIdBlockAddrCount[receiverPeerId];
            if (!blockAddCount.ContainsKey(blockAddress))
            {
                blockAddCount.Add(blockAddress, 1);
            }
            else
            {
                blockAddCount[blockAddress]++;
            }
        }
    }
}
