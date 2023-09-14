using KubernetesWorkflow;

namespace Core
{
    public sealed class CoreInterface
    {
        private readonly EntryPoint entryPoint;

        internal CoreInterface(EntryPoint entryPoint)
        {
            this.entryPoint = entryPoint;
        }

        public T GetPlugin<T>() where T : IProjectPlugin
        {
            return entryPoint.GetPlugin<T>();
        }

        public IDownloadedLog DownloadLog(IHasContainer containerSource, int? tailLines = null)
        {
            return DownloadLog(containerSource.Container, tailLines);
        }

        public IDownloadedLog DownloadLog(RunningContainer container, int? tailLines = null)
        {
            var workflow = entryPoint.Tools.CreateWorkflow();
            var file = entryPoint.Tools.GetLog().CreateSubfile();
            var logHandler = new LogDownloadHandler(container.Name, file);
            workflow.DownloadContainerLog(container, logHandler, tailLines);
            return logHandler.DownloadLog();
        }
    }

    public interface IHasContainer
    {
        RunningContainer Container { get; }
    }
}
