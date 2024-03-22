using CodexContractsPlugin;
using Logging;
using System.Numerics;
using Utils;

namespace CodexPlugin
{
    public class StoragePurchase : MarketplaceType
    {
        public StoragePurchase(ContentId cid)
        {
            ContentId = cid;
        }

        public ContentId ContentId { get; set; }
        public TestToken PricePerSlotPerSecond { get; set; } = 1.TestTokens();
        public TestToken RequiredCollateral { get; set; } = 1.TestTokens();
        public uint MinRequiredNumberOfNodes { get; set; }
        public uint NodeFailureTolerance { get; set; }
        public int ProofProbability { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan Expiry { get; set; }

        public CodexSalesRequestStorageRequest ToApiRequest()
        {
            return new CodexSalesRequestStorageRequest
            {
                duration = ToDecInt(Duration.TotalSeconds),
                proofProbability = ToDecInt(ProofProbability),
                reward = ToDecInt(PricePerSlotPerSecond),
                collateral = ToDecInt(RequiredCollateral),
                expiry = ToDecInt(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Expiry.TotalSeconds),
                nodes = MinRequiredNumberOfNodes,
                tolerance = NodeFailureTolerance
            };
        }

        public void Log(ILog log)
        {
            log.Log($"Requesting storage for: {ContentId.Id}... (" +
                $"pricePerSlotPerSecond: {PricePerSlotPerSecond}, " +
                $"requiredCollateral: {RequiredCollateral}, " +
                $"minRequiredNumberOfNodes: {MinRequiredNumberOfNodes}, " +
                $"nodeFailureTolerance: {NodeFailureTolerance}, " +
                $"proofProbability: {ProofProbability}, " +
                $"expiry: {Time.FormatDuration(Expiry)}, " +
                $"duration: {Time.FormatDuration(Duration)})");
        }
    }

    public class StorageAvailability : MarketplaceType
    {
        public StorageAvailability(ByteSize totalSpace, TimeSpan maxDuration, TestToken minPriceForTotalSpace, TestToken maxCollateral)
        {
            TotalSpace = totalSpace;
            MaxDuration = maxDuration;
            MinPriceForTotalSpace = minPriceForTotalSpace;
            MaxCollateral = maxCollateral;
        }

        public ByteSize TotalSpace { get; }
        public TimeSpan MaxDuration { get; }
        public TestToken MinPriceForTotalSpace { get; }
        public TestToken MaxCollateral { get; } 

        public CodexSalesAvailabilityRequest ToApiRequest()
        {
            return new CodexSalesAvailabilityRequest
            {
                size = ToDecInt(TotalSpace.SizeInBytes),
                duration = ToDecInt(MaxDuration.TotalSeconds),
                maxCollateral = ToDecInt(MaxCollateral),
                minPrice = ToDecInt(MinPriceForTotalSpace)
            };
        }

        public void Log(ILog log)
        {
            log.Log($"Making storage available... (" +
                $"size: {TotalSpace}, " +
                $"maxDuration: {Time.FormatDuration(MaxDuration)}, " + 
                $"minPriceForTotalSpace: {MinPriceForTotalSpace}, " +
                $"maxCollateral: {MaxCollateral})");
        }
    }

    public abstract class MarketplaceType
    {
        protected string ToDecInt(double d)
        {
            var i = new BigInteger(d);
            return i.ToString("D");
        }

        protected string ToDecInt(TestToken t)
        {
            var i = new BigInteger(t.Amount);
            return i.ToString("D");
        }
    }
}
