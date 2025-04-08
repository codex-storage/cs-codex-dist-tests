using Logging;
using Utils;

namespace CodexClient
{
    public class StoragePurchaseRequest
    {
        public StoragePurchaseRequest(ContentId cid)
        {
            ContentId = cid;
        }

        public ContentId ContentId { get; }
        public TestToken PricePerBytePerSecond { get; set; } = 1.TstWei();
        public TestToken CollateralPerByte { get; set; } = 1.TstWei();
        public uint MinRequiredNumberOfNodes { get; set; }
        public uint NodeFailureTolerance { get; set; }
        public int ProofProbability { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan Expiry { get; set; }

        public void Log(ILog log)
        {
            log.Log($"Requesting storage for: {ContentId.Id}... (" +
                $"pricePerBytePerSecond: {PricePerBytePerSecond}, " +
                $"collateralPerByte: {CollateralPerByte}, " +
                $"minRequiredNumberOfNodes: {MinRequiredNumberOfNodes}, " +
                $"nodeFailureTolerance: {NodeFailureTolerance}, " +
                $"proofProbability: {ProofProbability}, " +
                $"expiry: {Time.FormatDuration(Expiry)}, " +
                $"duration: {Time.FormatDuration(Duration)})");
        }
    }

    public class StoragePurchase
    {
        public StoragePurchaseState State { get; set; } = StoragePurchaseState.Unknown;
        public string Error { get; set; } = string.Empty;
        public StorageRequest Request { get; set; } = null!;

        public bool IsCancelled => State == StoragePurchaseState.Cancelled;
        public bool IsError => State == StoragePurchaseState.Errored;
        public bool IsFinished => State == StoragePurchaseState.Finished;
        public bool IsStarted => State == StoragePurchaseState.Started;
        public bool IsSubmitted => State == StoragePurchaseState.Submitted;
    }

    public enum StoragePurchaseState
    {
        Cancelled = 0,
        Errored = 1,
        Failed = 2,
        Finished = 3,
        Pending = 4,
        Started = 5,
        Submitted = 6,
        Unknown = 7,
    }

    public class StorageRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public StorageAsk Ask { get; set; } = null!;
        public StorageContent Content { get; set; } = null!;
        public long Expiry { get; set; }
        public string Nonce { get; set; } = string.Empty;
    }

    public class StorageAsk
    {
        public long Slots { get; set; }
        public long SlotSize { get; set; }
        public long Duration { get; set; }
        public string ProofProbability { get; set; } = string.Empty;
        public string PricePerBytePerSecond { get; set; } = string.Empty;
        public long MaxSlotLoss { get; set; }
    }

    public class StorageContent
    {
        public string Cid { get; set; } = string.Empty;
        //public ErasureParameters Erasure { get; set; }
        //public PoRParameters Por { get; set; }
    }

    public class StorageAvailability
    {
        public StorageAvailability(ByteSize totalSpace, TimeSpan maxDuration, TestToken minPricePerBytePerSecond, TestToken totalCollateral)
        {
            TotalSpace = totalSpace;
            MaxDuration = maxDuration;
            MinPricePerBytePerSecond = minPricePerBytePerSecond;
            TotalCollateral = totalCollateral;
        }

        public string Id { get; set; } = string.Empty;
        public ByteSize TotalSpace { get; }
        public TimeSpan MaxDuration { get; }
        public TestToken MinPricePerBytePerSecond { get; }
        public TestToken TotalCollateral { get; } 
        public ByteSize FreeSpace { get; set; } = ByteSize.Zero;

        public void Log(ILog log)
        {
            log.Log($"Storage Availability: (" +
                $"totalSize: {TotalSpace}, " +
                $"maxDuration: {Time.FormatDuration(MaxDuration)}, " + 
                $"minPricePerBytePerSecond: {MinPricePerBytePerSecond}, " +
                $"totalCollateral: {TotalCollateral})");
        }
    }
}
