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
        public void OnFileDownloaded(ContentId cid)
        {
        }

        public void OnFileUploaded(ContentId cid)
        {
        }

        public void OnNodeStarted(string name, string image)
        {
        }

        public void OnNodeStopping()
        {
        }
    }
}
