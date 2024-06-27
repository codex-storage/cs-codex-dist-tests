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

        public void OnNewRequest(RequestEvent requestEvent)
        {
            foreach (var handler in handlers) handler.OnNewRequest(requestEvent);
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
            foreach (var handler in handlers) handler.OnRequestCancelled(requestEvent);
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            foreach (var handler in handlers) handler.OnRequestFinished(requestEvent);
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            foreach (var handler in handlers) handler.OnRequestFulfilled(requestEvent);
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            foreach (var handler in handlers) handler.OnSlotFilled(requestEvent, host, slotIndex);
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            foreach (var handler in handlers) handler.OnSlotFreed(requestEvent, slotIndex);
        }
    }
}
