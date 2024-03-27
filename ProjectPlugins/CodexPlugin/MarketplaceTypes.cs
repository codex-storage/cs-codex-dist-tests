using CodexContractsPlugin;
using Logging;
using Utils;

namespace CodexPlugin
{
    public class StoragePurchaseRequest
    {
        public StoragePurchaseRequest(ContentId cid)
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

    public class StoragePurchase
    {
        public string State { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    public class StorageAvailability
    {
        public StorageAvailability(ByteSize totalSpace, TimeSpan maxDuration, TestToken minPriceForTotalSpace, TestToken maxCollateral)
        {
            TotalSpace = totalSpace;
            MaxDuration = maxDuration;
            MinPriceForTotalSpace = minPriceForTotalSpace;
            MaxCollateral = maxCollateral;
        }

        public string Id { get; set; } = string.Empty;
        public ByteSize TotalSpace { get; }
        public TimeSpan MaxDuration { get; }
        public TestToken MinPriceForTotalSpace { get; }
        public TestToken MaxCollateral { get; } 

        public void Log(ILog log)
        {
            log.Log($"Making storage available... (" +
                $"totalSize: {TotalSpace}, " +
                $"maxDuration: {Time.FormatDuration(MaxDuration)}, " + 
                $"minPriceForTotalSpace: {MinPriceForTotalSpace}, " +
                $"maxCollateral: {MaxCollateral})");
        }
    }
}
