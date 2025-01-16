using Logging;

namespace CodexClient
{
    public interface IIProcessControlFactory
    {
        IProcessControl CreateProcessControl(ICodexInstance instance);
    }

    public interface IProcessControl
    {
        void Stop(bool waitTillStopped);
        IDownloadedLog DownloadLog(LogFile file);
        void DeleteDataDirFolder();
        bool HasCrashed();
    }
}
