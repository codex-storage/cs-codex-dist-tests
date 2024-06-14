using CodexContractsPlugin.Marketplace;
using Logging;

namespace CodexContractsPlugin.ChainMonitor
{
    public interface IChainStateRequest
    {
        Request Request { get; }
        RequestState State { get; }
        DateTime ExpiryUtc { get; }
        DateTime FinishedUtc { get; }
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
        }

        public Request Request { get; }
        public RequestState State { get; private set; }
        public DateTime ExpiryUtc { get; }
        public DateTime FinishedUtc { get; }

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
}
