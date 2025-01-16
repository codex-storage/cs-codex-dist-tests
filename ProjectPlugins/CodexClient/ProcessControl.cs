using Logging;

namespace CodexClient
{
    public interface IProcessControl
    {
        void Stop(ICodexInstance instance, bool waitTillStopped);
        IDownloadedLog DownloadLog(ICodexInstance instance, LogFile file);
        void DeleteDataDirFolder(ICodexInstance instance);
    }
}
