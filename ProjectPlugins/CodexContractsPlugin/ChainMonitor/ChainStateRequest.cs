using CodexContractsPlugin.Marketplace;
using Logging;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public interface IChainStateRequest
    {
        Request Request { get; }
        RequestState State { get; }
        DateTime ExpiryUtc { get; }
        DateTime FinishedUtc { get; }
        EthAddress Client { get; }
        RequestHosts Hosts { get; }
    }

    public class ChainStateRequest : IChainStateRequest
    {
        private readonly ILog log;

        public ChainStateRequest(ILog log, Request request, RequestState state)
        {
            this.log = log;
            Request = request;
            State = state;

            ExpiryUtc = request.Block.Utc + TimeSpan.FromSeconds((double)request.Expiry);
            FinishedUtc = request.Block.Utc + TimeSpan.FromSeconds((double)request.Ask.Duration);

            Log($"[{request.Block.BlockNumber}] Created as {State}.");

            Client = new EthAddress(request.Client);
            Hosts = new RequestHosts();
        }

        public Request Request { get; }
        public RequestState State { get; private set; }
        public DateTime ExpiryUtc { get; }
        public DateTime FinishedUtc { get; }
        public EthAddress Client { get; }
        public RequestHosts Hosts { get; }

        public void UpdateState(ulong blockNumber, RequestState newState)
        {
            Log($"[{blockNumber}] Transit: {State} -> {newState}");
            State = newState;
        }

        public void Log(string msg)
        {
            log.Log($"Request '{Request.Id}': {msg}");
        }
    }

    public class RequestHosts
    {
        private readonly Dictionary<int, EthAddress> hosts = new Dictionary<int, EthAddress>();
        private readonly List<int> filled = new List<int>();

        public void HostFillsSlot(EthAddress host, int index)
        {
            hosts.Add(index, host);
            filled.Add(index);
        }

        public bool IsFilled(int index)
        {
            return hosts.ContainsKey(index);
        }

        public bool WasPreviouslyFilled(int index)
        {
            return filled.Contains(index);
        }
        
        public void SlotFreed(int index)
        {
            hosts.Remove(index);
        }

        public EthAddress? GetHost(int index)
        {
            if (!hosts.ContainsKey(index)) return null;
            return hosts[index];
        }

        public EthAddress[] GetHosts()
        {
            return hosts.Values.ToArray();
        }
    }
}
