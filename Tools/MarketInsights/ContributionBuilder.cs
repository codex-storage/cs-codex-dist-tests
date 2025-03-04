using BlockchainUtils;
using CodexContractsPlugin.ChainMonitor;
using GethPlugin;
using Logging;
using System.Numerics;
using Utils;

namespace MarketInsights
{
    public class ContributionBuilder : IChainStateChangeHandler
    {
        private readonly MarketTimeSegment segment = new MarketTimeSegment();
        private readonly ILog log;

        public ContributionBuilder(ILog log, TimeRange timeRange)
        {
            segment = new MarketTimeSegment
            {
                FromUtc = timeRange.From,
                ToUtc = timeRange.To
            };
            this.log = log;
        }

        public void OnNewRequest(RequestEvent requestEvent)
        {
            AddRequestToAverage(segment.Submitted, requestEvent);
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
            AddRequestToAverage(segment.Expired, requestEvent);
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
            AddRequestToAverage(segment.Failed, requestEvent);
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            AddRequestToAverage(segment.Finished, requestEvent);
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            AddRequestToAverage(segment.Started, requestEvent);
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

        public void OnProofSubmitted(BlockTimeEntry block, string id)
        {
        }

        public void OnError(string msg)
        {
            log.Error(msg);
        }

        public MarketTimeSegment GetSegment()
        {
            return segment;
        }

        private void AddRequestToAverage(ContractAverages average, RequestEvent requestEvent)
        {
            average.Number++;
            average.PricePerBytePerSecond = GetNewAverage(average.PricePerBytePerSecond, average.Number, requestEvent.Request.Request.Ask.PricePerBytePerSecond);
            average.Size = GetNewAverage(average.Size, average.Number, requestEvent.Request.Request.Ask.SlotSize);
            average.Duration = GetNewAverage(average.Duration, average.Number, requestEvent.Request.Request.Ask.Duration);
            average.CollateralPerByte = GetNewAverage(average.CollateralPerByte, average.Number, requestEvent.Request.Request.Ask.CollateralPerByte);
            average.ProofProbability = GetNewAverage(average.ProofProbability, average.Number, requestEvent.Request.Request.Ask.ProofProbability);
        }

        private float GetNewAverage(float currentAverage, int newNumberOfValues, ulong newValue)
        {
            return GetNewAverage(currentAverage, newNumberOfValues, Convert.ToSingle(newValue));
        }

        private float GetNewAverage(float currentAverage, int newNumberOfValues, BigInteger newValue)
        {
            return GetNewAverage(currentAverage, newNumberOfValues, (float)newValue);
        }

        private float GetNewAverage(float currentAverage, int newNumberOfValues, float newValue)
        {
            return RollingAverage.GetNewAverage(currentAverage, newNumberOfValues, newValue);
        }
    }
}
