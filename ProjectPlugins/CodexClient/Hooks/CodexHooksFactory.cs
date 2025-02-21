using Utils;

namespace CodexClient.Hooks
{
    public interface ICodexHooksProvider
    {
        ICodexNodeHooks CreateHooks(string nodeName);
    }

    public class CodexHooksFactory
    {
        public List<ICodexHooksProvider> Providers { get; } = new List<ICodexHooksProvider>();

        public ICodexNodeHooks CreateHooks(string nodeName)
        {
            if (Providers.Count == 0) return new DoNothingCodexHooks();

            var hooks = Providers.Select(p => p.CreateHooks(nodeName)).ToArray();
            return new MuxingCodexNodeHooks(hooks);
        }
    }

    public class DoNothingHooksProvider : ICodexHooksProvider
    {
        public ICodexNodeHooks CreateHooks(string nodeName)
        {
            return new DoNothingCodexHooks();
        }
    }

    public class DoNothingCodexHooks : ICodexNodeHooks
    {
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
