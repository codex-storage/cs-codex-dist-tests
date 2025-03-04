using BlockchainUtils;
using System.Numerics;
using Utils;

namespace CodexContractsPlugin.ChainMonitor
{
    public class DoNothingChainEventHandler : IChainStateChangeHandler
    {
        public void OnNewRequest(RequestEvent requestEvent)
        {
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
        }

        public void OnSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
        }

        public void OnError(string msg)
        {
        }

        public void OnProofSubmitted(BlockTimeEntry block, string id)
        {
        }
    }
}
