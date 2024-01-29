using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using GethPlugin;

namespace TestNetRewarder
{
    public class HistoricState
    {
        private readonly List<StorageRequest> storageRequests = new List<StorageRequest>();

        public StorageRequest[] StorageRequests { get { return storageRequests.ToArray(); } }

        public void ProcessNewRequests(Request[] requests)
        {
            storageRequests.AddRange(requests.Select(r => new StorageRequest(r)));
        }

        public void UpdateStorageRequests(ICodexContracts contracts)
        {
            foreach (var r in storageRequests) r.Update(contracts);
        }
    }

    public class StorageRequest
    {
        public StorageRequest(Request request)
        {
            Request = request;
            Hosts = Array.Empty<EthAddress>();
        }

        public Request Request { get; }
        public EthAddress[] Hosts { get; private set; }
        public RequestState State { get; private set; }
        public bool RecentlyStarted { get; private set; }
        public bool RecentlyFininshed { get; private set; }

        public void Update(ICodexContracts contracts)
        {
            Hosts = GetHosts(contracts);

            var newState = contracts.GetRequestState(Request);

            RecentlyStarted =
                State == RequestState.New &&
                newState == RequestState.Started;

            RecentlyFininshed =
                State == RequestState.Started &&
                newState == RequestState.Finished;

            State = newState;
        }

        private EthAddress[] GetHosts(ICodexContracts contracts)
        {
            var result = new List<EthAddress>();

            for (decimal i = 0; i < Request.Ask.Slots; i++)
            {
                var host = contracts.GetSlotHost(Request, i);
                if (host != null) result.Add(host);
            }

            return result.ToArray();
        }
    }
}
