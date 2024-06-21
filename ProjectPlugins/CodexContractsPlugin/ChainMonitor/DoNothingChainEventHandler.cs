using System.Numerics;

namespace CodexContractsPlugin.ChainMonitor
{
    public class DoNothingChainEventHandler : IChainStateChangeHandler
    {
        public void OnNewRequest(IChainStateRequest request)
        {
        }

        public void OnRequestCancelled(IChainStateRequest request)
        {
        }

        public void OnRequestFinished(IChainStateRequest request)
        {
        }

        public void OnRequestFulfilled(IChainStateRequest request)
        {
        }

        public void OnSlotFilled(IChainStateRequest request, BigInteger slotIndex)
        {
        }

        public void OnSlotFreed(IChainStateRequest request, BigInteger slotIndex)
        {
        }
    }
}
