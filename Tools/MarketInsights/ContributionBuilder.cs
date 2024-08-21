using CodexContractsPlugin.ChainMonitor;
using GethPlugin;
using System.Numerics;
using Utils;

namespace MarketInsights
{
    public class ContributionBuilder : IChainStateChangeHandler
    {
        private readonly MarketTimeSegment segment = new MarketTimeSegment();

        public ContributionBuilder(TimeRange timeRange)
        {
            segment = new MarketTimeSegment
            {
                FromUtc = timeRange.From,
                ToUtc = timeRange.To
            };
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

        public MarketTimeSegment GetSegment()
        {
            return segment;
        }

        private void AddRequestToAverage(ContractAverages average, RequestEvent requestEvent)
        {
            average.Number++;
            average.Price = GetNewAverage(average.Price, average.Number, requestEvent.Request.Request.Ask.Reward);
            average.Size = GetNewAverage(average.Size, average.Number, requestEvent.Request.Request.Ask.SlotSize);
            average.Duration = GetNewAverage(average.Duration, average.Number, requestEvent.Request.Request.Ask.Duration);
            average.Collateral = GetNewAverage(average.Collateral, average.Number, requestEvent.Request.Request.Ask.Collateral);
            average.ProofProbability = GetNewAverage(average.ProofProbability, average.Number, requestEvent.Request.Request.Ask.ProofProbability);
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
