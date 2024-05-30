using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Newtonsoft.Json;

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

        public void CleanUpOldRequests()
        {
            storageRequests.RemoveAll(r =>
                r.State == RequestState.Cancelled ||
                r.State == RequestState.Finished ||
                r.State == RequestState.Failed
            );
        }

        public string EntireString()
        {
            return JsonConvert.SerializeObject(StorageRequests);
        }

        public HistoricState()
        {
        }

        public HistoricState(StorageRequest[] requests)
        {
            storageRequests.AddRange(requests);
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
        
        [JsonIgnore]
        public bool RecentlyStarted { get; private set; }

        [JsonIgnore]
        public bool RecentlyFinished { get; private set; }

        public void Update(ICodexContracts contracts)
        {
            var newHosts = GetHosts(contracts);

            var newState = contracts.GetRequestState(Request);

            RecentlyStarted =
                State == RequestState.New &&
                newState == RequestState.Started;

            RecentlyFinished =
                State == RequestState.Started &&
                newState == RequestState.Finished;

            State = newState;
            Hosts = newHosts;
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
