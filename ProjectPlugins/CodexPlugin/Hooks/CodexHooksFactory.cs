using Utils;

namespace CodexPlugin.Hooks
{
    public interface ICodexHooksProvider
    {
        ICodexNodeHooks CreateHooks(string nodeName);
    }

    public class CodexHooksFactory
    {
        public ICodexHooksProvider Provider { get; set; } = new DoNothingHooksProvider();

        public ICodexNodeHooks CreateHooks(string nodeName)
        {
            return Provider.CreateHooks(nodeName);
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

        public void OnNodeStarted(string peerId, string nodeId)
        {
        }

        public void OnNodeStarting(DateTime startUtc, string image)
        {
        }

        public void OnNodeStopping()
        {
        }
    }
}
