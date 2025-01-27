using CodexClient;

namespace CodexPlugin
{
    public interface ICodexStarter : IProcessControlFactory
    {
        string GetCodexId();
        string GetCodexRevision();
        ICodexInstance[] BringOnline(CodexSetup codexSetup);
        ICodexNodeGroup WrapCodexContainers(ICodexInstance[] instances);
    }
}
