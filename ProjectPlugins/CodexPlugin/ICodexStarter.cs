using CodexClient;

namespace CodexPlugin
{
    public interface ICodexStarter
    {
        ICodexInstance[] BringOnline(CodexSetup codexSetup);
        void Decommission();
    }
}
