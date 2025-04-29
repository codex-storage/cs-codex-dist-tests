using CodexClient;
using CodexClient.Hooks;
using Utils;

namespace CodexTests
{
    public class CodexLogTrackerProvider  : ICodexHooksProvider
    {
        private readonly Action<ICodexNode> addNode;

        public CodexLogTrackerProvider(Action<ICodexNode> addNode)
        {
            this.addNode = addNode;
        }

        // See TestLifecycle.cs DownloadAllLogs()
        public ICodexNodeHooks CreateHooks(string nodeName)
        {
            return new CodexLogTracker(addNode);
        }

        public class CodexLogTracker : ICodexNodeHooks
        {
            private readonly Action<ICodexNode> addNode;

            public CodexLogTracker(Action<ICodexNode> addNode)
            {
                this.addNode = addNode;
            }

            public void OnFileDownloaded(ByteSize size, ContentId cid)
            {
            }

            public void OnFileDownloading(ContentId cid)
            {
            }

            public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
            {
            }

            public void OnFileUploading(string uid, ByteSize size)
            {
            }

            public void OnNodeStarted(ICodexNode node, string peerId, string nodeId)
            {
                addNode(node);
            }

            public void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount)
            {
            }

            public void OnNodeStopping()
            {
            }

            public void OnStorageAvailabilityCreated(StorageAvailability response)
            {
            }

            public void OnStorageContractSubmitted(StoragePurchaseContract storagePurchaseContract)
            {
            }

            public void OnStorageContractUpdated(StoragePurchase purchaseStatus)
            {
            }
        }
    }
}
