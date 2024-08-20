using GethPlugin;
using Utils;

namespace CodexPlugin.Hooks
{
    public interface ICodexNodeHooks
    {
        void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount);
        void OnNodeStarted(string peerId, string nodeId);
        void OnNodeStopping();
        void OnFileUploading(string uid, ByteSize size);
        void OnFileUploaded(string uid, ByteSize size, ContentId cid);
        void OnFileDownloading(ContentId cid);
        void OnFileDownloaded(ByteSize size, ContentId cid);
        void OnStorageContractSubmitted(StoragePurchaseContract storagePurchaseContract);
        void OnStorageContractUpdated(StoragePurchase purchaseStatus);
        void OnStorageAvailabilityCreated(StorageAvailability response);
    }
}
