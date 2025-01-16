using CodexPlugin.Hooks;
using OverwatchTranscript;
using Utils;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexNodeTranscriptWriter : ICodexNodeHooks
    {
        private readonly ITranscriptWriter writer;
        private readonly IdentityMap identityMap;
        private readonly string name;
        private int identityIndex = -1;
        private readonly List<(DateTime, OverwatchCodexEvent)> pendingEvents = new List<(DateTime, OverwatchCodexEvent)>();

        public CodexNodeTranscriptWriter(ITranscriptWriter writer, IdentityMap identityMap, string name)
        {
            this.writer = writer;
            this.identityMap = identityMap;
            this.name = name;
        }

        public void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount)
        {
            WriteCodexEvent(startUtc, e =>
            {
                e.NodeStarting = new NodeStartingEvent
                {
                    Image = image,
                    EthAddress = ethAccount != null ? ethAccount.ToString() : ""
                };
            });
        }

        public void OnNodeStarted(string peerId, string nodeId)
        {
            if (string.IsNullOrEmpty(peerId) || string.IsNullOrEmpty(nodeId))
            {
                throw new Exception("Node started - peerId and/or nodeId unknown.");
            }

            identityMap.Add(name, peerId, nodeId);
            identityIndex = identityMap.GetIndex(name);

            WriteCodexEvent(e =>
            {
                e.NodeStarted = new NodeStartedEvent
                {
                };
            });
        }

        public void OnNodeStopping()
        {
            WriteCodexEvent(e =>
            {
                e.NodeStopping = new NodeStoppingEvent
                {
                };
            });
        }

        public void OnFileDownloading(ContentId cid)
        {
            WriteCodexEvent(e =>
            {
                e.FileDownloading = new FileDownloadingEvent
                {
                    Cid = cid.Id
                };
            });
        }

        public void OnFileDownloaded(ByteSize size, ContentId cid)
        {
            WriteCodexEvent(e =>
            {
                e.FileDownloaded = new FileDownloadedEvent
                {
                    Cid = cid.Id,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        public void OnFileUploading(string uid, ByteSize size)
        {
            WriteCodexEvent(e =>
            {
                e.FileUploading = new FileUploadingEvent
                {
                    UniqueId = uid,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
        {
            WriteCodexEvent(e =>
            {
                e.FileUploaded = new FileUploadedEvent
                { 
                    UniqueId = uid,
                    Cid = cid.Id,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        public void OnStorageContractSubmitted(StoragePurchaseContract storagePurchaseContract)
        {
            WriteCodexEvent(e =>
            {
                e.StorageContractSubmitted = new StorageContractSubmittedEvent
                {
                    PurchaseId = storagePurchaseContract.PurchaseId,
                    PurchaseRequest = storagePurchaseContract.Purchase
                };
            });
        }

        public void OnStorageContractUpdated(StoragePurchase purchaseStatus)
        {
            WriteCodexEvent(e =>
            {
                e.StorageContractUpdated = new StorageContractUpdatedEvent
                {
                    StoragePurchase = purchaseStatus
                };
            });
        }

        public void OnStorageAvailabilityCreated(StorageAvailability response)
        {
            WriteCodexEvent(e =>
            {
                e.StorageAvailabilityCreated = new StorageAvailabilityCreatedEvent
                {
                    StorageAvailability = response
                };
            });
        }

        private void WriteCodexEvent(Action<OverwatchCodexEvent> action)
        {
            WriteCodexEvent(DateTime.UtcNow, action);
        }

        private void WriteCodexEvent(DateTime utc, Action<OverwatchCodexEvent> action)
        {
            var e = new OverwatchCodexEvent
            {
                NodeIdentity = identityIndex
            };

            action(e);

            if (identityIndex < 0)
            {
                // If we don't know our id, don't write the events yet.
                AddToCache(utc, e);
            }
            else
            {
                e.Write(utc, writer);

                // Write any events that we cached when we didn't have our id yet.
                WriteAndClearCache();
            }
        }

        private void AddToCache(DateTime utc, OverwatchCodexEvent e)
        {
            pendingEvents.Add((utc, e));
        }

        private void WriteAndClearCache()
        {
            if (pendingEvents.Any())
            {
                foreach (var pair in pendingEvents)
                {
                    pair.Item2.NodeIdentity = identityIndex;
                    pair.Item2.Write(pair.Item1, writer);
                }
                pendingEvents.Clear();
            }
        }
    }
}
