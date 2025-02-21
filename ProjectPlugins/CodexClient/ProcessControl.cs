using Logging;

namespace CodexClient
{
    public interface IProcessControlFactory
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

    public class DoNothingProcessControlFactory : IProcessControlFactory
    {
        public IProcessControl CreateProcessControl(ICodexInstance instance)
        {
            return new DoNothingProcessControl();
        }
    }

    public class DoNothingProcessControl : IProcessControl
    {
        public void DeleteDataDirFolder()
        {
        }

        public IDownloadedLog DownloadLog(LogFile file)
        {
            throw new NotImplementedException("Not supported by DoNothingProcessControl");
        }

        public bool HasCrashed()
        {
            return false;
        }

        public void Stop(bool waitTillStopped)
        {
        }
    }
}
