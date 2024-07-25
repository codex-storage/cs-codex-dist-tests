namespace OverwatchTranscript
{
    [Serializable]
    public partial class OverwatchHeader
    {
        public OverwatchCodexHeader? CodexHeader { get; set; }
    }

    [Serializable]
    public partial class OverwatchEvent
    {
        public OverwatchCodexEvent? CodexEvent { get; set; }
    }

    [Serializable]
    public class OverwatchCodexHeader
    {
        public int TotalNumberOfNodes { get; set; }
    }

    [Serializable]
    public class OverwatchCodexEvent
    {
        public NodeStartedEvent? NodeStarted { get; set; }
        public NodeStoppedEvent? NodeStopped { get; set; }
        public FileUploadedEvent? FileUploaded { get; set; }
        public FileDownloadedEvent? FileDownloaded { get; set; }
        public BlockReceivedEvent? BlockReceived { get; set; }
    }

    #region Scenario Generated Events

    [Serializable]
    public class NodeStartedEvent
    {
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Args { get; set; } = string.Empty;
    }

    [Serializable]
    public class NodeStoppedEvent
    {
        public string Name { get; set; } = string.Empty;
    }

    [Serializable]
    public class FileUploadedEvent
    {
        public ulong ByteSize { get; set; }
        public string Cid { get; set; } = string.Empty;
    }

    [Serializable]
    public class FileDownloadedEvent
    {
        public string Cid { get; set; } = string.Empty;
    }

    #endregion

    #region Codex Generated Events

    [Serializable]
    public class BlockReceivedEvent
    {
        public string BlockAddress { get; set; } = string.Empty;
        public string PeerId { get; set; } = string.Empty;
    }

    #endregion
}
