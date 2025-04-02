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
        public string State { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public StorageRequest Request { get; set; } = null!;

        public bool IsCancelled => State.ToLowerInvariant().Contains("cancel");
        public bool IsError => State.ToLowerInvariant().Contains("error");
        public bool IsFinished => State.ToLowerInvariant().Contains("finished");
        public bool IsStarted => State.ToLowerInvariant().Contains("started");
        public bool IsSubmitted => State.ToLowerInvariant().Contains("submitted");
    }

    public enum StoragePurchaseState
    {
        Cancelled = 0,
        Error = 1,
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
        public string Expiry { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
    }

    public class StorageAsk
    {
        public int Slots { get; set; }
        public string SlotSize { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string ProofProbability { get; set; } = string.Empty;
        public string PricePerBytePerSecond { get; set; } = string.Empty;
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
