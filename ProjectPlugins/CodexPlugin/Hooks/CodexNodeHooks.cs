using Utils;

namespace CodexPlugin.Hooks
{
    public interface ICodexNodeHooks
    {
        void OnNodeStarting(DateTime startUtc, string image);
        void OnNodeStarted(string peerId);
        void OnNodeStopping();
        void OnFileUploading(string uid, ByteSize size);
        void OnFileUploaded(string uid, ByteSize size, ContentId cid);
        void OnFileDownloading(ContentId cid);
        void OnFileDownloaded(ByteSize size, ContentId cid);
    }
}
