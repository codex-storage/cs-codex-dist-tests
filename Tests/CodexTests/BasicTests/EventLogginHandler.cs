using CodexContractsPlugin.ChainMonitor;
using GethPlugin;
using Logging;
using System.Numerics;

namespace CodexTests.BasicTests
{
    public class EventLogginHandler : IChainStateChangeHandler
    {
        private readonly ILog log;

        public EventLogginHandler(ILog log)
        {
            this.log = log;
        }

        public void OnNewRequest(RequestEvent requestEvent)
        {
            Log(nameof(OnNewRequest), requestEvent);
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
            Log(nameof(OnRequestCancelled), requestEvent);
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
            Log(nameof(OnRequestFailed), requestEvent);
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            Log(nameof(OnRequestFinished), requestEvent);
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            Log(nameof(OnRequestFulfilled), requestEvent);
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            Log(nameof(OnSlotFilled), requestEvent, host.ToString(), slotIndex.ToString());
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            Log(nameof(OnNewRequest), requestEvent, slotIndex.ToString());
        }

        private void Log(string name, object o, params string[] str)
        {
            log.Log(name + ": " + o.ToString() + " - " + string.Join(",", str));
        }
    }
}