using BlockchainUtils;
using CodexContractsPlugin.Marketplace;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public interface IChainStateRequest
    {
        byte[] RequestId { get; }
        public BlockTimeEntry Block { get; }
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

        public ChainStateRequest(ILog log, byte[] requestId, BlockTimeEntry block, Request request, RequestState state)
        {
            if (requestId == null || requestId.Length != 32) throw new ArgumentException(nameof(requestId));

            this.log = log;
            RequestId = requestId;
            Block = block;
            Request = request;
            State = state;

            ExpiryUtc = Block.Utc + TimeSpan.FromSeconds((double)request.Expiry);
            FinishedUtc = Block.Utc + TimeSpan.FromSeconds((double)request.Ask.Duration);

            Log($"[{Block.BlockNumber}] Created as {State}.");

            Client = new EthAddress(request.Client);
            Hosts = new RequestHosts();
        }

        public byte[] RequestId { get; }
        public BlockTimeEntry Block { get; }
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
            log.Log($"Request '{RequestId.ToHex()}': {msg}");
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
