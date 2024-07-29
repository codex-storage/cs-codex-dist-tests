namespace CodexPlugin.OverwatchSupport
{
    [Serializable]
    public class OverwatchCodexHeader
    {
        public int TotalNumberOfNodes { get; set; }
    }

    [Serializable]
    public class OverwatchCodexEvent
    {
        public string PeerId { get; set; } = string.Empty;
        public ScenarioFinishedEvent? ScenarioFinished { get; set; }
        public NodeStartingEvent? NodeStarting { get; set; }
        public NodeStartedEvent? NodeStarted { get; set; }
        public NodeStoppedEvent? NodeStopped { get; set; }
        public BootstrapConfigEvent? BootstrapConfig { get; set; }
        public FileUploadedEvent? FileUploaded { get; set; }
        public FileDownloadedEvent? FileDownloaded { get; set; }
        public BlockReceivedEvent? BlockReceived { get; set; }
    }

    #region Scenario Generated Events

    [Serializable]
    public class ScenarioFinishedEvent
    {
        public bool Success { get; set; }
        public string Result { get; set; } = string.Empty;
    }

    [Serializable]
    public class NodeStartingEvent
    {
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }

    [Serializable]
    public class NodeStartedEvent
    {
    }

    [Serializable]
    public class NodeStoppedEvent
    {
        public string Name { get; set; } = string.Empty;
    }

    [Serializable]
    public class BootstrapConfigEvent
    {
        public string BootstrapPeerId { get; set; } = string.Empty;
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
        public string SenderPeerId { get; set; } = string.Empty;
    }

    #endregion
}
