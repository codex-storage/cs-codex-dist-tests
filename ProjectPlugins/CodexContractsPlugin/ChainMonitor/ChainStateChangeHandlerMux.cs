using BlockchainUtils;
using System.Numerics;
using Utils;

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

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex, bool isRepair)
        {
            foreach (var handler in Handlers) handler.OnSlotFilled(requestEvent, host, slotIndex, isRepair);
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            foreach (var handler in Handlers) handler.OnSlotFreed(requestEvent, slotIndex);
        }

        public void OnSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
            foreach (var handler in Handlers) handler.OnSlotReservationsFull(requestEvent, slotIndex);
        }

        public void OnError(string msg)
        {
            foreach (var handler in Handlers) handler.OnError(msg);
        }

        public void OnProofSubmitted(BlockTimeEntry block, string id)
        {
            foreach (var handler in Handlers) handler.OnProofSubmitted(block, id);
        }
    }
}
