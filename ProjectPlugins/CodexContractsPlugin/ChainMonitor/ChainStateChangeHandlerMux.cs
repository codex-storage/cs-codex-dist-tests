using GethPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CodexContractsPlugin.ChainMonitor
{
    public class ChainStateChangeHandlerMux : IChainStateChangeHandler
    {
        public ChainStateChangeHandlerMux(params IChainStateChangeHandler[] handlers)
        {
            Handlers = handlers.ToList();
        }

        public List<IChainStateChangeHandler> Handlers { get; } = new List<IChainStateChangeHandler>();

        public void OnNewRequest(RequestEvent requestEvent)
        {
            foreach (var handler in Handlers) handler.OnNewRequest(requestEvent);
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
            foreach (var handler in Handlers) handler.OnRequestCancelled(requestEvent);
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
            foreach (var handler in Handlers) handler.OnRequestFailed(requestEvent);
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            foreach (var handler in Handlers) handler.OnRequestFinished(requestEvent);
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            foreach (var handler in Handlers) handler.OnRequestFulfilled(requestEvent);
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            foreach (var handler in Handlers) handler.OnSlotFilled(requestEvent, host, slotIndex);
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            foreach (var handler in Handlers) handler.OnSlotFreed(requestEvent, slotIndex);
        }
    }
}
