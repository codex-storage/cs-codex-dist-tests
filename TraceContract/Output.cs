using System.Numerics;
using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using Logging;
using Utils;

namespace TraceContract
{
    public class Output
    {
        private readonly ILog log;

        public Output(ILog log)
        {
            this.log = log;
        }

        public void LogRequestCreated(RequestEvent requestEvent)
        {
            throw new NotImplementedException();
        }

        public void LogRequestCancelled(RequestEvent requestEvent)
        {
            throw new NotImplementedException();
        }

        public void LogRequestFailed(RequestEvent requestEvent)
        {
            throw new NotImplementedException();
        }

        public void LogRequestFinished(RequestEvent requestEvent)
        {
            throw new NotImplementedException();
        }

        public void LogRequestStarted(RequestEvent requestEvent)
        {
            throw new NotImplementedException();
        }

        public void LogSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            throw new NotImplementedException();
        }

        public void LogSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            throw new NotImplementedException();
        }

        public void LogSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
            throw new NotImplementedException();
        }

        public void LogReserveSlotCalls(ReserveSlotFunction[] reserveSlotFunctions)
        {
            throw new NotImplementedException();
        }

        public void WriteContractEvents()
        {
            throw new NotImplementedException();
        }
    }
}
