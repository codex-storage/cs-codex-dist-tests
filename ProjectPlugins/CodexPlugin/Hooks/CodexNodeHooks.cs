namespace CodexPlugin.Hooks
{
    public interface ICodexNodeHooks
    {
        void OnNodeStarted(string peerId, string image);
        void OnNodeStopping();
        void OnFileUploaded(ContentId cid);
        void OnFileDownloaded(ContentId cid);
    }
}
