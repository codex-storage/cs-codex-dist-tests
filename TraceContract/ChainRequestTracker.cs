using System.Numerics;
using BlockchainUtils;
using CodexContractsPlugin.ChainMonitor;
using Utils;

namespace TraceContract
{
    public class ChainRequestTracker : IChainStateChangeHandler
    {
        private readonly string requestId;
        private readonly Output output;

        public ChainRequestTracker(Output output, string requestId)
        {
            this.requestId = requestId.ToLowerInvariant();
            this.output = output;
        }

        public bool IsFinished { get; private set; } = false;
        public DateTime FinishUtc { get; private set; } = DateTime.MinValue;

        public void OnError(string msg)
        {
        }

        public void OnNewRequest(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent)) output.LogRequestCreated(requestEvent);
        }

        public void OnProofSubmitted(BlockTimeEntry block, string id)
        {
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent))
            {
                IsFinished = true;
                FinishUtc = requestEvent.Block.Utc;
                output.LogRequestCancelled(requestEvent);
            }
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent))
            {
                IsFinished = true;
                FinishUtc = requestEvent.Block.Utc;
                output.LogRequestFailed(requestEvent);
            }
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent))
            {
                IsFinished = true;
                FinishUtc = requestEvent.Block.Utc;
                output.LogRequestFinished(requestEvent);
            }
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            if (IsMyRequest(requestEvent))
            {
                output.LogRequestStarted(requestEvent);
            }
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            if (IsMyRequest(requestEvent))
            {
                output.LogSlotFilled(requestEvent, host, slotIndex);
            }
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            if (IsMyRequest(requestEvent))
            {
                output.LogSlotFreed(requestEvent, slotIndex);
            }
        }

        public void OnSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
            if (IsMyRequest(requestEvent))
            {
                output.LogSlotReservationsFull(requestEvent, slotIndex);
            }
        }

        private bool IsMyRequest(RequestEvent requestEvent)
        {
            return requestId == requestEvent.Request.Request.Id.ToLowerInvariant();
        }
    }

}
