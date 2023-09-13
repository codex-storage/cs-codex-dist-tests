using CodexPlugin;
using Logging;
using static DistTestCore.Helpers.FullConnectivityHelper;

namespace DistTestCore.Helpers
{
    public class PeerConnectionTestHelpers : IFullConnectivityImplementation
    {
        private readonly FullConnectivityHelper helper;

        public PeerConnectionTestHelpers(ILog log)
        {
            helper = new FullConnectivityHelper(log, this);
        }

        public void AssertFullyConnected(IEnumerable<IOnlineCodexNode> nodes)
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
            foreach (var peer in entry.Response.table.nodes)
            {
                var expected = GetExpectedDiscoveryEndpoint(allEntries, peer);
                if (expected != peer.address)
                {
                    result += $"Node:{entry.Node.GetName()} has incorrect peer table entry. Was: '{peer.address}', expected: '{expected}'. ";
                }
            }
            return result;
        }

        public PeerConnectionState Check(Entry from, Entry to)
        {
            var peerId = to.Response.id;

            var response = from.Node.GetDebugPeer(peerId);
            if (!response.IsPeerFound)
            {
                return PeerConnectionState.NoConnection;
            }
            if (!string.IsNullOrEmpty(response.peerId) && response.addresses.Any())
            {
                return PeerConnectionState.Connection;
            }
            return PeerConnectionState.Unknown;
        }

        private static string GetExpectedDiscoveryEndpoint(Entry[] allEntries, CodexDebugTableNodeResponse node)
        {
            var peer = allEntries.SingleOrDefault(e => e.Response.table.localNode.peerId == node.peerId);
            if (peer == null) return $"peerId: {node.peerId} is not known.";

            var container = peer.Node.Container;
            var ip = container.Pod.PodInfo.Ip;
            var discPort = container.Recipe.GetPortByTag(CodexContainerRecipe.DiscoveryPortTag)!;
            return $"{ip}:{discPort.Number}";
        }
    }
}
