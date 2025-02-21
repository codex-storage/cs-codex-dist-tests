﻿using CodexClient;
using OverwatchTranscript;

namespace CodexPlugin.OverwatchSupport
{
    [Serializable]
    public class OverwatchCodexHeader
    {
        public CodexNodeIdentity[] Nodes { get; set; } = Array.Empty<CodexNodeIdentity>();
    }

    [Serializable]
    public class OverwatchCodexEvent
    {
        public int NodeIdentity { get; set; } = -1;
        public ScenarioFinishedEvent? ScenarioFinished { get; set; }
        public NodeStartingEvent? NodeStarting { get; set; }
        public NodeStartedEvent? NodeStarted { get; set; }
        public NodeStoppingEvent? NodeStopping { get; set; }
        public BootstrapConfigEvent? BootstrapConfig { get; set; }
        public FileUploadingEvent? FileUploading { get; set; }
        public FileUploadedEvent? FileUploaded { get; set; }
        public FileDownloadingEvent? FileDownloading { get; set; }
        public FileDownloadedEvent? FileDownloaded { get; set; }
        public BlockReceivedEvent? BlockReceived { get; set; }
        public PeerDialSuccessfulEvent? DialSuccessful { get; set; }
        public PeerDroppedEvent? PeerDropped { get; set; }
        public StorageContractSubmittedEvent? StorageContractSubmitted { get; set; }
        public StorageContractUpdatedEvent? StorageContractUpdated { get; set; }
        public StorageAvailabilityCreatedEvent? StorageAvailabilityCreated { get; set; }

        public void Write(DateTime utc, ITranscriptWriter writer)
        {
            if (NodeIdentity == -1 && ScenarioFinished == null)
            {
                throw new Exception("NodeIdentity not set, and event is not ScenarioFinished.");
            }
            if (AllNull()) throw new Exception("No event data was set");

            writer.Add(utc, this);
        }

        private bool AllNull()
        {
            var props = GetType()
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.PropertyType != typeof(string)).ToArray();

            return props.All(p => p.GetValue(this) == null);
        }
    }

    [Serializable]
    public class CodexNodeIdentity
    {
        public string Name { get; set; } = string.Empty;
        public string PeerId { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public float KademliaNormalizedPosition { get; set; } = 0.0f;
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
        public string Image { get; set; } = string.Empty;
        public string EthAddress { get; set; } = string.Empty;
    }

    [Serializable]
    public class NodeStartedEvent
    {
    }

    [Serializable]
    public class NodeStoppingEvent
    {
    }

    [Serializable]
    public class BootstrapConfigEvent
    {
        public string BootstrapPeerId { get; set; } = string.Empty;
    }

    [Serializable]
    public class FileUploadingEvent
    {
        public string UniqueId { get; set;} = string.Empty;
        public long ByteSize { get; set; }
    }

    [Serializable]
    public class FileDownloadingEvent
    {
        public string Cid { get; set; } = string.Empty;
    }

    [Serializable]
    public class FileUploadedEvent
    {
        public string UniqueId { get; set; } = string.Empty;
        public long ByteSize { get; set; }
        public string Cid { get; set; } = string.Empty;
    }

    [Serializable]
    public class FileDownloadedEvent
    {
        public string Cid { get; set; } = string.Empty;
        public long ByteSize { get; set; }
    }

    [Serializable]
    public class StorageAvailabilityCreatedEvent
    {
        public StorageAvailability StorageAvailability { get; set; } = null!;
    }

    [Serializable]
    public class StorageContractUpdatedEvent
    {
        public StoragePurchase StoragePurchase { get; set; } = null!;
    }

    [Serializable]
    public class StorageContractSubmittedEvent
    {
        public string PurchaseId { get; set; } = string.Empty;
        public StoragePurchaseRequest PurchaseRequest { get; set; } = null!;
    }

    #endregion

    #region Codex Generated Events

    [Serializable]
    public class BlockReceivedEvent
    {
        public string BlockAddress { get; set; } = string.Empty;
        public string SenderPeerId { get; set; } = string.Empty;
    }

    [Serializable]
    public class PeerDialSuccessfulEvent
    {
        public string TargetPeerId { get; set; } = string.Empty;
    }

    [Serializable]
    public class PeerDroppedEvent
    {
        public string DroppedPeerId { get; set; } = string.Empty;
    }

    #endregion
}
