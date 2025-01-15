namespace CodexPlugin
{
    public interface IProcessControl
    {
        void Stop(ICodexInstance instance, bool waitTillStopped);
    }
}
