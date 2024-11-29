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
        public TestToken PricePerSlotPerSecond { get; set; } = 1.TstWei();
        public TestToken RequiredCollateral { get; set; } = 1.TstWei();
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
        public StorageRequest Request { get; set; } = null!;
    }

    public class StorageRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public StorageAsk Ask { get; set; } = null!;
        public StorageContent Content { get; set; } = null!;
        public string Expiry { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
    }

    public class StorageAsk
    {
        public int Slots { get; set; }
        public string SlotSize { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string ProofProbability { get; set; } = string.Empty;
        public string Reward { get; set; } = string.Empty;
        public int MaxSlotLoss { get; set; }
    }

    public class StorageContent
    {
        public string Cid { get; set; } = string.Empty;
        //public ErasureParameters Erasure { get; set; }
        //public PoRParameters Por { get; set; }
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
        public ByteSize FreeSpace { get; set; } = ByteSize.Zero;

        public void Log(ILog log)
        {
            log.Log($"Storage Availability: (" +
                $"totalSize: {TotalSpace}, " +
                $"maxDuration: {Time.FormatDuration(MaxDuration)}, " + 
                $"minPriceForTotalSpace: {MinPriceForTotalSpace}, " +
                $"maxCollateral: {MaxCollateral})");
        }
    }
}
