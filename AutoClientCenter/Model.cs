namespace AutoClientCenter
{
    public class AcTasks
    {
        public TimeSpan StartTaskEvery { get; set; } = TimeSpan.FromHours(6);
        public AcTask[] Tasks { get; set; } = Array.Empty<AcTask>();
    }

    public class AcTask
    {
        public int ChanceWeight { get; set; }
        public AcTaskStep[] Steps { get; set; } = Array.Empty<AcTaskStep>();
    }

    public class AcTaskStep
    {
        public string Id { get; set; } = string.Empty;
        public AcUploadStep? UploadStep { get; set; }
        public AcStoreStep? StoreStep { get; set; }
        public AcDownloadStep? DownloadStep { get; set; }
        public string? ResultErrorMsg { get; set; }
    }

    public class AcUploadStep
    {
        public long SizeInBytes { get; set; }
        public string? ResultCid { get; set; }
    }

    public class AcStoreStep
    {
        public int ContractDurationMinutes { get; set; }
        public int ContractExpiryMinutes { get; set; }
        public int NumHosts { get; set; }
        public int HostTolerance { get; set; }
        public int Price { get; set; }
        public int RequiredCollateral { get; set; }
        public string? ResultPurchaseId { get; set; }
        public string? ResultCid { get; set; }
    }

    public class AcDownloadStep
    {
        public string[] Cids { get; set; } = Array.Empty<string>();
        public long[] ResultDownloadTimeSeconds { get; set; } = Array.Empty<long>();
    }

    public class AcStats
    {
        public int NumberOfAutoClients { get; set; } todo send client peerId

        public DateTime ServiceStartUtc { get; set; } = DateTime.MinValue;
        public int TotalUploads { get; set; }
        public int TotalUploadsFailed { get; set; }
        public int TotalDownloads { get; set; }
        public long[] DownloadTimesSeconds { get; set; } = Array.Empty<long>();
        public int TotalDownloadsFailed { get; set; }
        public int TotalContractsStarted { get; set; }
        public int TotalContractsCompleted { get; set; }
        public int TotalContractsExpired { get; set; }
        public int TotalContractsFailed { get; set; }
    }
}
