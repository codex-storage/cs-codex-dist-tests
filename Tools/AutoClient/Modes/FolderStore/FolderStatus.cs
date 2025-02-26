namespace AutoClient.Modes.FolderStore
{
    [Serializable]
    public class FolderStatus
    {
        public List<FileStatus> Files { get; set; } = new List<FileStatus>();
    }

    [Serializable]
    public class FileStatus
    {
        public string Filename { get; set; } = string.Empty;
        public string BasicCid { get; set; } = string.Empty;
        public string EncodedCid { get; set; } = string.Empty;
        public string PurchaseId { get; set; } = string.Empty;
        public DateTime PurchaseFinishedUtc { get; set; } = DateTime.MinValue;
    }
}
