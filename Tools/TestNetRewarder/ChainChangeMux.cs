using CodexContractsPlugin.ChainMonitor;
using GethPlugin;
using System.Numerics;

namespace TestNetRewarder
{
    public class ChainChangeMux : IChainStateChangeHandler
    {
        private readonly IChainStateChangeHandler[] handlers;

        public ChainChangeMux(params IChainStateChangeHandler[] handlers)
        {
            this.handlers = handlers;
        }

        public void OnNewRequest(IChainStateRequest request)
        {
            foreach (var handler in handlers) handler.OnNewRequest(request);
        }

        public void OnRequestCancelled(IChainStateRequest request)
        {
            foreach (var handler in handlers) handler.OnRequestCancelled(request);
        }

        public void OnRequestFinished(IChainStateRequest request)
        {
            foreach (var handler in handlers) handler.OnRequestFinished(request);
        }

        public void OnRequestFulfilled(IChainStateRequest request)
        {
            foreach (var handler in handlers) handler.OnRequestFulfilled(request);
        }

        public void OnSlotFilled(IChainStateRequest request, EthAddress host, BigInteger slotIndex)
        {
            foreach (var handler in handlers) handler.OnSlotFilled(request, host, slotIndex);
        }

        public void OnSlotFreed(IChainStateRequest request, BigInteger slotIndex)
        {
            foreach (var handler in handlers) handler.OnSlotFreed(request, slotIndex);
        }
    }
}
