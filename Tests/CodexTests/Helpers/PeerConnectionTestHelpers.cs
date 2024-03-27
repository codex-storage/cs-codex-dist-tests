using CodexPlugin;
using Logging;
using static CodexTests.Helpers.FullConnectivityHelper;

namespace CodexTests.Helpers
{
    public class PeerConnectionTestHelpers : IFullConnectivityImplementation
    {
        private readonly FullConnectivityHelper helper;

        public PeerConnectionTestHelpers(ILog log)
        {
            helper = new FullConnectivityHelper(log, this);
        }

        public void AssertFullyConnected(IEnumerable<ICodexNode> nodes)
        {
            helper.AssertFullyConnected(nodes);
        }

        public string Description()
        {
            return "Peer Discovery";
        }

        public string ValidateEntry(Entry entry, Entry[] allEntries)
        {
            var result = string.Empty;
            foreach (var peer in entry.Response.Table.Nodes)
            {
                var expected = GetExpectedDiscoveryEndpoint(allEntries, peer);
                if (expected != peer.Address)
                {
                    result += $"Node:{entry.Node.GetName()} has incorrect peer table entry. Was: '{peer.Address}', expected: '{expected}'. ";
                }
            }
            return result;
        }

        public PeerConnectionState Check(Entry from, Entry to)
        {
            var peerId = to.Response.Id;

            var response = from.Node.GetDebugPeer(peerId);
            if (!response.IsPeerFound)
            {
                return PeerConnectionState.NoConnection;
            }
            if (!string.IsNullOrEmpty(response.PeerId) && response.Addresses.Any())
            {
                return PeerConnectionState.Connection;
            }
            return PeerConnectionState.Unknown;
        }

        private static string GetExpectedDiscoveryEndpoint(Entry[] allEntries, DebugInfoTableNode node)
        {
            var peer = allEntries.SingleOrDefault(e => e.Response.Table.LocalNode.PeerId == node.PeerId);
            if (peer == null) return $"peerId: {node.PeerId} is not known.";

            var container = peer.Node.Container;
            var podInfo = peer.Node.GetPodInfo();
            var ip = podInfo.Ip;
            var discPort = container.Recipe.GetPortByTag(CodexContainerRecipe.DiscoveryPortTag)!;
            return $"{ip}:{discPort.Number}";
        }
    }
}
