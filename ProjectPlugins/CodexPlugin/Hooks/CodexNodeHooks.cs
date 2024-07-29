namespace CodexPlugin.Hooks
{
    public interface ICodexNodeHooks
    {
        void OnNodeStarting(DateTime startUtc, string image);
        void OnNodeStarted(string peerId);
        void OnNodeStopping();
        void OnFileUploaded(ContentId cid);
        void OnFileDownloaded(ContentId cid);
    }
}
