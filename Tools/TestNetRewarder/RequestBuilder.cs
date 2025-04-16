using CodexContractsPlugin.ChainMonitor;
using DiscordRewards;
using Utils;

namespace TestNetRewarder
{
    public class RequestBuilder
    {
        public EventsAndErrors Build(ChainState chainState, ChainEventMessage[] lines, string[] errors)
        {
            var activeChainAddresses = CollectActiveAddresses(chainState);

            return new EventsAndErrors
            {
                EventsOverview = lines,
                Errors = errors,
                ActiveChainAddresses = activeChainAddresses
            };
        }

        private ActiveChainAddresses CollectActiveAddresses(ChainState chainState)
        {
            var hosts = new List<string>();
            var clients = new List<string>();

            foreach (var request in chainState.Requests)
            {
                CollectAddresses(request, hosts, clients);
            }

            return new ActiveChainAddresses
            {
                Hosts = hosts.ToArray(),
                Clients = clients.ToArray()
            };
        }

        private void CollectAddresses(IChainStateRequest request, List<string> hosts, List<string> clients)
        {
            if (request.State != CodexContractsPlugin.RequestState.Started) return;

            AddIfNew(clients, request.Client);
            foreach (var host in request.Hosts.GetHosts())
            {
                AddIfNew(hosts, host);
            }
        }

        private void AddIfNew(List<string> list, EthAddress address)
        {
            var addr = address.Address;
            if (!list.Contains(addr)) list.Add(addr);
        }
    }
}
