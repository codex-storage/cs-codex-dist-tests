using CodexPlugin.Hooks;
using OverwatchTranscript;
using Utils;

namespace CodexPlugin.OverwatchSupport
{
    public class CodexNodeTranscriptWriter : ICodexNodeHooks
    {
        private readonly ITranscriptWriter writer;
        private readonly NameIdMap nameIdMap;
        private readonly string name;
        private CodexNodeIdentity identity = new CodexNodeIdentity();
        private readonly List<(DateTime, OverwatchCodexEvent)> pendingEvents = new List<(DateTime, OverwatchCodexEvent)>();

        public CodexNodeTranscriptWriter(ITranscriptWriter writer, NameIdMap nameIdMap, string name)
        {
            this.writer = writer;
            this.nameIdMap = nameIdMap;
            this.name = name;
        }

        public void OnNodeStarting(DateTime startUtc, string image)
        {
            WriteCodexEvent(startUtc, e =>
            {
                e.NodeStarting = new NodeStartingEvent
                {
                    Image = image
                };
            });
        }

        public void OnNodeStarted(string peerId, string nodeId)
        {
            identity.PeerId = peerId;
            identity.NodeId = nodeId;

            if (string.IsNullOrEmpty(peerId) || string.IsNullOrEmpty(nodeId))
            {
                throw new Exception("Node started - peerId and/or nodeId unknown.");
            }

            nameIdMap.Add(name, new CodexNodeIdentity
            {
                PeerId = peerId,
                NodeId = nodeId
            });

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

        private void WriteCodexEvent(Action<OverwatchCodexEvent> action)
        {
            WriteCodexEvent(DateTime.UtcNow, action);
        }

        private void WriteCodexEvent(DateTime utc, Action<OverwatchCodexEvent> action)
        {
            var e = new OverwatchCodexEvent
            {
                Name = name,
                Identity = identity
            };

            action(e);

            if (string.IsNullOrEmpty(identity.PeerId))
            {
                // If we don't know our peerId, don't write the events yet.
                AddToCache(utc, e);
            }
            else
            {
                e.Write(utc, writer);

                // Write any events that we cached when we didn't have our peerId yet.
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
                    pair.Item2.Identity = identity;
                    pair.Item2.Write(pair.Item1, writer);
                }
                pendingEvents.Clear();
            }
        }
    }
}
