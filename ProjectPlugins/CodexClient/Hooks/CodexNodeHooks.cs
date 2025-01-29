using Utils;

namespace CodexClient.Hooks
{
    public interface ICodexNodeHooks
    {
        void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount);
        void OnNodeStarted(ICodexNode node, string peerId, string nodeId);
        void OnNodeStopping();
        void OnFileUploading(string uid, ByteSize size);
        void OnFileUploaded(string uid, ByteSize size, ContentId cid);
        void OnFileDownloading(ContentId cid);
        void OnFileDownloaded(ByteSize size, ContentId cid);
        void OnStorageContractSubmitted(StoragePurchaseContract storagePurchaseContract);
        void OnStorageContractUpdated(StoragePurchase purchaseStatus);
        void OnStorageAvailabilityCreated(StorageAvailability response);
    }

    public class MuxingCodexNodeHooks : ICodexNodeHooks
    {
        private readonly ICodexNodeHooks[] backingHooks;

        public MuxingCodexNodeHooks(ICodexNodeHooks[] backingHooks)
        {
            this.backingHooks = backingHooks;
        }

        public void OnFileDownloaded(ByteSize size, ContentId cid)
        {
            foreach (var h in backingHooks) h.OnFileDownloaded(size, cid);
        }

        public void OnFileDownloading(ContentId cid)
        {
            foreach (var h in backingHooks) h.OnFileDownloading(cid);
        }

        public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
        {
            foreach (var h in backingHooks) h.OnFileUploaded(uid, size, cid);
        }

        public void OnFileUploading(string uid, ByteSize size)
        {
            foreach (var h in backingHooks) h.OnFileUploading(uid, size);
        }

        public void OnNodeStarted(ICodexNode node, string peerId, string nodeId)
        {
            foreach (var h in backingHooks) h.OnNodeStarted(node, peerId, nodeId);
        }

        public void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount)
        {
            foreach (var h in backingHooks) h.OnNodeStarting(startUtc, image, ethAccount);
        }

        public void OnNodeStopping()
        {
            foreach (var h in backingHooks) h.OnNodeStopping();
        }

        public void OnStorageAvailabilityCreated(StorageAvailability response)
        {
            foreach (var h in backingHooks) h.OnStorageAvailabilityCreated(response);
        }

        public void OnStorageContractSubmitted(StoragePurchaseContract storagePurchaseContract)
        {
            foreach (var h in backingHooks) h.OnStorageContractSubmitted(storagePurchaseContract);
        }

        public void OnStorageContractUpdated(StoragePurchase purchaseStatus)
        {
            foreach (var h in backingHooks) h.OnStorageContractUpdated(purchaseStatus);
        }
    }
}
